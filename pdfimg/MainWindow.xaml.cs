using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

using System.IO;//必須
using Windows.Data.Pdf;
using Microsoft.Win32;
using System.Windows.Media;

//下の2つを参照に追加する必要がある
//"C:\Program Files (x86)\Windows Kits\8.1\References\CommonConfiguration\Neutral\Annotated\Windows.winmd"
//"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5\System.Runtime.WindowsRuntime.dll"

namespace PDFCon
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private enum ColorMode { Full, Gray, TwoTone }

        // PDF 情報をまとめて保持するレコード
        private record PdfInfo(PdfDocument Doc, string Directory, string Name);

        // ドロップされた複数 PDF を格納
        private readonly List<PdfInfo> _pdfList = new();

        // 共通設定
        private double _dpi = 1200;        // 出力 DPI
        private const int DefaultJpegQuality = 90;
        private int _previewIndex = 0; // プレビュー中の PDF インデックス

        // プレビュー用のオリジナルサイズ保持
        private uint _originalPixelWidth;
        private uint _originalPixelHeight;

        // A4 200fpi: 幅(px) = (mm ÷ 25.4) × 200
        private const double A4PortraitWidthPx200 = 210.0 / 25.4 * 200;  // ≒ 1653
        private const double A4LandscapeWidthPx200 = 297.0 / 25.4 * 200;  // ≒ 2338

        // A4 350fpi: 幅(px) = (mm ÷ 25.4) × 350
        private const double A4PortraitWidthPx = 210.0 / 25.4 * 350;  // ≒ 2894
        private const double A4LandscapeWidthPx = 297.0 / 25.4 * 350;  // ≒ 4093

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            // フォーマット選択にも同じイベントをフック
            FormatComboBox.SelectionChanged += FormatOrColor_SelectionChanged;

            // ドラッグ＆ドロップを有効化
            this.Title = "PDFCon";
            AllowDrop = true;
            Drop += MainWindow_Drop;
            //ImageContainer.SizeChanged += ImageContainer_SizeChanged;
            ImageContainer.SizeChanged += (_, __) => UpdateImageMaxSize();

            // スライダー初期化
            DpiSlider.Value = _dpi;
            tbDpi.Text = _dpi.ToString("F0");
        }

        // --------------------  UI  --------------------
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageMaxSize();
        // private void ImageContainer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateImageMaxSize();


        // Image no MaxWidth/MaxHeightをコンテナサイズに合わせる
        private void UpdateImageMaxSize()
        {
            MyImage.MaxWidth = ImageContainer.ActualWidth;
            MyImage.MaxHeight = ImageContainer.ActualHeight;
            UpdateZoomText();
        }

        // ズーム率(表示幅 ÷ 元画像)を計算して表示
        private void UpdateZoomText()
        {
            if (_originalPixelWidth == 0) return;
            double zoom = MyImage.ActualWidth / _originalPixelWidth;
            tbZoom.Text = $"ズーム率 : {zoom:P0}";
        }

        // --------------------  Drag & Drop  --------------------
        private async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
            _pdfList.Clear();

            foreach (string path in paths)
            {
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
                var doc = await PdfDocument.LoadFromFileAsync(file);
                _pdfList.Add(new PdfInfo(doc,
                                         System.IO.Path.GetDirectoryName(path)!,
                                         System.IO.Path.GetFileNameWithoutExtension(path)!));
            }

            if (_pdfList.Count == 0) return;

            // 先頭ファイルをプレビュー
            _previewIndex = 0;
            DisplayImage(_pdfList[0].Doc, 0, _dpi);
            tbPageCount.Text = $"読み込みファイル数 : {_pdfList.Count}";
        }


        // --------------------  Preview  --------------------
        //PDFファイルを画像に変換して表示
        //高さ＝Height
        //幅＝width
        // PDF→画像レンダリング後に元サイズを保存しておく
        private async void DisplayImage(PdfDocument doc, int pageIndex, double dpi)
        {
            using PdfPage page = doc.GetPage((uint)pageIndex);

            double destHeight = (dpi / page.Size.Width) * page.Size.Height;
            var options = new PdfPageRenderOptions
            {
                DestinationWidth = (uint)Math.Round(dpi),
                DestinationHeight = (uint)Math.Round(destHeight, MidpointRounding.AwayFromZero)
            };

            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream, options);

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream.AsStream();
            image.EndInit();

            // 元のピクセル寸法をキャッシュ
            _originalPixelWidth = (uint)image.PixelWidth;
            _originalPixelHeight = (uint)image.PixelHeight;

            MyImage.Source = image;
            // 幅・高さの明治設定は外す。　Stretch = Uniform が効くようになる。
            MyImage.Width = double.NaN; // Stretch=Uniform を効かせる
            MyImage.Height = double.NaN;

            // カラーモード適応
            var converted = ApplyColorMode(image);
            MyImage.Source = converted;

            // ラベルにも出しておく
            tbWidth.Text = $"横ピクセル : {_originalPixelWidth}";
            tbHeight.Text = $"縦ピクセル : {_originalPixelHeight}";

            // 初期倍率は 100%
            tbZoom.Text = "ズーム率: 100%";

            // 今のコンテナ状況に合わせて最大サイズを更新
            UpdateImageMaxSize();
        }

        // --------------------  DPI スライダー  --------------------
        private void DpiSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded) return; // コンストラクタ中の呼び出しを無視
            SetDpi(Math.Round(e.NewValue / 100) * 100);
        }

        private void SetDpi(double dpi)
        {
            _dpi = dpi;
            tbDpi.Text = dpi.ToString("F0");
            // 設定に保存
            pdfimg.Properties.Settings.Default.LastDpi = (int)dpi;
            pdfimg.Properties.Settings.Default.Save();

            //  スライダーと同期（ボタン等から変更されたときのみ）
            if (DpiSlider.Value != _dpi) DpiSlider.Value = _dpi; // ボタンから変更されたとき同期
            RefreshPreview();
        }
        private void RefreshPreview()
        {
            if (_pdfList.Count > 0)
                DisplayImage(_pdfList[_previewIndex].Doc, 0, _dpi);
        }

        private void textbox101_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (double.TryParse(textbox101.Text, out double dpi))
            {
                _dpi = dpi;
                RefreshPreview();
            }
        }

        // --------------------  Save  --------------------
        private async void ButtonSave_Click(object _, RoutedEventArgs __)
        {
            if (_pdfList.Count == 0) return;

            string format = ((ComboBoxItem)FormatComboBox.SelectedItem).Content.ToString()!;
            bool isJpeg = format.Equals("JPEG", StringComparison.OrdinalIgnoreCase);

            if (MessageBox.Show(
                    $"選択形式: {format}\nDPI : {_dpi}\n\n変換を開始しますか？",
                    "確認", MessageBoxButton.OKCancel, MessageBoxImage.Information) != MessageBoxResult.OK)
                return;

            IsEnabled = false;
            try
            {
                foreach (var info in _pdfList)
                {
                    int digits = info.Doc.PageCount.ToString().Length; // 連番桁数
                    var tasks = Enumerable.Range(0, (int)info.Doc.PageCount)
                        .Select(i => SavePageCoreAsync(info, i, _dpi, digits,
                        format, isJpeg ? DefaultJpegQuality : null,
                        (int)info.Doc.PageCount))
                        .ToList();

                    await Task.WhenAll(tasks); // 同一 PDF 内は並列処理
                }

                MessageBox.Show($"すべての PDF を {format} に変換しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラー: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private Task SavePageCoreAsync(PdfInfo info, int pageIndex, double dpi, int digits, string format, int? jpegQuality, int pageCount)
            => SavePageCore(info.Doc, dpi, info.Directory, info.Name, pageIndex, digits, format, jpegQuality, pageCount);


        /// <summary>
        /// jpeg画像で保存
        /// </summary>
        /// <param name="pdfDocument">読み込んだPDF、Windows.Data.Pdf.PdfDocument</param>
        /// <param name="dpi">PDFファイルを読み込む時のDPI</param>
        /// <param name="directory">保存フォルダ</param>
        /// <param name="baseName">保存名</param>
        /// <param name="pageIndex">保存するPDFのページ</param>
        /// <param name="jpegQuality">jpegの品質min0、max100</param>
        /// <returns></returns>
        private static async Task SavePageCore(PdfDocument pdfDocument, double dpi, string directory, string baseName, int pageIndex, int digits, string format, int? jpegQuality, int pageCount)
        {
            using PdfPage page = pdfDocument.GetPage((uint)pageIndex);

            double destHeight = (dpi / page.Size.Width) * page.Size.Height;
            var options = new PdfPageRenderOptions
            {
                //指定されたdpiを元に画像サイズ指定、四捨五入
                DestinationWidth = (uint)Math.Round(dpi),
                DestinationHeight = (uint)Math.Round(destHeight, MidpointRounding.AwayFromZero),

            };
            using var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream();
            await page.RenderToStreamAsync(stream, options);
            
            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = stream.AsStream();
            bmp.EndInit();

            
            // カラーモード適用

            BitmapSource final;
            switch (pdfimg.Properties.Settings.Default.LastColorMode)
            {
                case "グレースケール":
                    final = new FormatConvertedBitmap(bmp, PixelFormats.Gray8, null, 0);
                    break;
                case "白黒":
                    // （上と同様に WriteableBitmap で閾値処理…）
                    var gray = new FormatConvertedBitmap(bmp, PixelFormats.Gray8, null, 0);
                    var wb = new WriteableBitmap(gray);
                    int w = wb.PixelWidth, h = wb.PixelHeight;
                    int stride = w;
                    var pixels = new byte[h * stride];
                    wb.CopyPixels(pixels, stride, 0);
                    // 閾値：128 を超えれば白 (255)、それ以外は黒 (0)
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = (pixels[i] > 128) ? (byte)255 : (byte)0;
                    wb.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);
                    final = wb;
                    break;
                default:
                    final = bmp;
                    break;
            }

            BitmapEncoder encoder;
            string ext;
            switch (format.ToUpperInvariant())
            {
                case "PNG":
                    encoder = new PngBitmapEncoder();
                    ext = ".png";
                    break;
                case "TIFF":
                    encoder = new TiffBitmapEncoder();
                    ext = ".tif";
                    break;
                case "JPEG":
                default:
                    var jenc = new JpegBitmapEncoder();
                    jenc.QualityLevel = jpegQuality ?? 90;
                    encoder = jenc;
                    ext = ".jpg";
                    break;
            }
            encoder.Frames.Add(BitmapFrame.Create(final));



            string seq = (pageIndex + 1).ToString("D" + digits);
            string path = pageCount == 1
              ? Path.Combine(directory, $"{baseName}{ext}")      // ←★1ページなら連番なし
              : Path.Combine(directory, $"{baseName}_{seq}{ext}");

            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            encoder.Save(fs);
        }
        // --------------------  その他 UI メンテ  --------------------
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e) { }

        private void clip_Click(object sender, RoutedEventArgs e)
        {
            MyImage.Source = null;
            _pdfList.Clear();
            Clipboard.Clear();
        }

        private void LoadSettings()
        {
            // 拡張子(フォーマット)
            var fmt = pdfimg.Properties.Settings.Default.LastFormat;
            foreach (ComboBoxItem item in FormatComboBox.Items)
                if((string)item.Content == fmt)
                    FormatComboBox.SelectedItem = item;

            // カラーモード
            var cm = pdfimg.Properties.Settings.Default.LastColorMode;
            foreach (ComboBoxItem item in FormatComboBox.Items)
                if((string)item.Content == cm)
                    FormatComboBox.SelectedItem = item;

            // DPI スライダー
            int lastDpi = pdfimg.Properties.Settings.Default.LastDpi;
            // スライダー値をセット → ValueChanged で SetDpi() が呼ばれる
            DpiSlider.Value = lastDpi;
        }

        // フォーマット or カラーモード変更時に呼ばれる
        private void FormatOrColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // 設定を保存
            pdfimg.Properties.Settings.Default.LastFormat =
                ((ComboBoxItem)FormatComboBox.SelectedItem).Content.ToString();
            pdfimg.Properties.Settings.Default.LastColorMode =
                ((ComboBoxItem)ColorModeComboBox.SelectedItem).Content.ToString();
            pdfimg.Properties.Settings.Default.Save();

            // プレじゅーを更新
            RefreshPreview();
        }

        // プレビュー表示の最後にカラーモードを適用
        private BitmapSource ApplyColorMode(BitmapSource source)
        {
            var mode = (string)((ComboBoxItem)ColorModeComboBox.SelectedItem).Content;
            switch (mode)
            {
                case "グレースケール":
                    // PixelFormats.Gray8 に変換
                    return new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);
                case "白黒":
                    // まずグレースケール化
                    var gray = new FormatConvertedBitmap(source, PixelFormats.Gray8, null, 0);
                    // WriteableBitmap へコピーして閾値処理
                    var wb = new WriteableBitmap(gray);
                    int w = wb.PixelWidth, h = wb.PixelHeight;
                    int stride = w;  // Gray8 => 1byte/pixel
                    var pixels = new byte[h * stride];
                    wb.CopyPixels(pixels, stride, 0);
                    for (int i = 0; i < pixels.Length; i++)
                        pixels[i] = (pixels[i] > 128) ? (byte)255 : (byte)0;
                    wb.WritePixels(new Int32Rect(0, 0, w, h), pixels, stride, 0);
                    return wb;
                default:
                    // フルカラーはそのまま
                    return source;

            }
        }
        private void ButtonA4Portrait200_Click(object sender, EventArgs e)
        {
            SetDpi(Math.Round(A4PortraitWidthPx200));
        }
        private void ButtonA4Landscape200_Click(object sender, EventArgs e)
        {
            SetDpi(Math.Round(A4LandscapeWidthPx200));
        }

        private void ButtonA4Portrait_Click(object sender, EventArgs e) 
        {
            SetDpi(Math.Round(A4PortraitWidthPx));
        }
        private void ButtonA4Landscape_Click(object sender, EventArgs e)
        {
            SetDpi(Math.Round(A4LandscapeWidthPx));
        }

        // About ボタンクリック
        private void ButtonAbout_Click(object sender, EventArgs e)
        {
            var about = new AboutWindow
            {
                Owner = this
            };
            about.ShowDialog();
        }
    }
}