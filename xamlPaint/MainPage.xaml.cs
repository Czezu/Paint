using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;
using Windows.Media.SpeechSynthesis;
using Windows.Media.Playback;
using Windows.Graphics.Imaging;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;



namespace xamlPaint
{

    public sealed partial class MainPage : Page
    {
        private Point punktPoczatkowy;
        private List<UIElement> aktualneRysowanie;
        private Point punktPoczatkowyProsta;
        private Line previewLine = null;

        private SolidColorBrush pedzel = new SolidColorBrush(Colors.Black);
        private bool czyRysuje = false;

        private Stack<List<UIElement>> historiaUndo = new Stack<List<UIElement>>();

        public MainPage()
        {
            this.InitializeComponent();
        }



        private void poleRysowania_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            Point pt = e.GetCurrentPoint(poleRysowania).Position;
            czyRysuje = true;
            if (rdbDowolna.IsChecked == true)
            {
                punktPoczatkowy = pt;
                aktualneRysowanie = new List<UIElement>();
            }
            else if (rdbProsta.IsChecked == true)
            {
                // Dla trybu "Prosta" zapisuje punkt początkowy i tworze linię podglądową
                punktPoczatkowyProsta = pt;
                previewLine = new Line
                {
                    X1 = punktPoczatkowyProsta.X,
                    Y1 = punktPoczatkowyProsta.Y,
                    X2 = punktPoczatkowyProsta.X,
                    Y2 = punktPoczatkowyProsta.Y,
                    Stroke = pedzel,
                    StrokeThickness = sldGrubosc.Value,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round
                };
                poleRysowania.Children.Add(previewLine);
            }
        }

        private void poleRysowania_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            {
                if (!czyRysuje) return;
                Point aktualnyPunkt = e.GetCurrentPoint(poleRysowania).Position;

                if (rdbDowolna.IsChecked == true)
                {
                    // Rysowanie dowolne – kolejne segmenty
                    Line linia = new Line
                    {
                        X1 = punktPoczatkowy.X,
                        Y1 = punktPoczatkowy.Y,
                        X2 = aktualnyPunkt.X,
                        Y2 = aktualnyPunkt.Y,
                        Stroke = pedzel,
                        StrokeThickness = sldGrubosc.Value,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    poleRysowania.Children.Add(linia);
                    aktualneRysowanie.Add(linia);
                    punktPoczatkowy = aktualnyPunkt;
                }
                else if (rdbProsta.IsChecked == true)
                {
                    // Aktualizacja tylko lini podglądowej
                    if (previewLine != null)
                    {
                        previewLine.X2 = aktualnyPunkt.X;
                        previewLine.Y2 = aktualnyPunkt.Y;
                    }
                }
            }
        }

        private void poleRysowania_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            czyRysuje = false;
            Point koncowyPunkt = e.GetCurrentPoint(poleRysowania).Position;

            if (rdbDowolna.IsChecked == true)
            {
                // Cała kreska do historii
                if (aktualneRysowanie != null && aktualneRysowanie.Count > 0)
                {
                    historiaUndo.Push(new List<UIElement>(aktualneRysowanie));
                    aktualneRysowanie.Clear();
                }
            }
            else if (rdbProsta.IsChecked == true)
            {
                // Usunięcie lini podglądowej, rysujemy finalną prostą linię
                if (previewLine != null)
                {
                    poleRysowania.Children.Remove(previewLine);
                    previewLine = null;
                }
                if (punktPoczatkowyProsta != koncowyPunkt)
                {
                    Line linia = new Line
                    {
                        X1 = punktPoczatkowyProsta.X,
                        Y1 = punktPoczatkowyProsta.Y,
                        X2 = koncowyPunkt.X,
                        Y2 = koncowyPunkt.Y,
                        Stroke = pedzel,
                        StrokeThickness = sldGrubosc.Value,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    };
                    poleRysowania.Children.Add(linia);
                    historiaUndo.Push(new List<UIElement> { linia });
                }
            }
        }

        private void btnUndo_Click(object sender, RoutedEventArgs e)
        {
            if (historiaUndo.Count > 0)
            {
                List<UIElement> ostatnieRysowanie = historiaUndo.Pop();
                foreach (UIElement element in ostatnieRysowanie)
                {
                    poleRysowania.Children.Remove(element);
                }
            }
        }

        private void Rectangle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Rectangle rect)
            {
                if (rect.Fill is SolidColorBrush brush)
                {
                    pedzel = new SolidColorBrush(brush.Color);
                }
            }
        }

        private void colorPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
        {
            pedzel = new SolidColorBrush(args.NewColor);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }
        private TaskCompletionSource<bool> mediaEndedTcs;

        private async Task OdtworzInformacjeGlosowa(string tekst)
        {
            using (var syntezator = new SpeechSynthesizer())
            {
                SpeechSynthesisStream strumien = await syntezator.SynthesizeTextToStreamAsync(tekst);

                mediaPlayer.SetSource(strumien, strumien.ContentType);
                mediaPlayer.Play();
            }
        }
        private const string ConfirmationMessage = "Czy na pewno chcesz zamknąć program?";
        private async Task<ContentDialogResult> ShowConfirmationDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "Potwierdzenie",
                Content = ConfirmationMessage,
                PrimaryButtonText = "Tak",
                SecondaryButtonText = "Nie"
            };

            return await dialog.ShowAsync();
        }


        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(poleRysowania); // Renderuje poleRysowania do obrazu

            // StorageFile i FileSavePicker, lokalizacja i format zapisu
            FileSavePicker savePicker = new FileSavePicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            savePicker.SuggestedFileName = "Rysunek";
            savePicker.FileTypeChoices.Add("Obraz PNG", new[] { ".png" });
            savePicker.FileTypeChoices.Add("Obraz JPG", new[] { ".jpg" });

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.ReadWrite);

                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);

                var pixels = await renderTargetBitmap.GetPixelsAsync();
                var pixelBuffer = pixels.ToArray();

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                                      (uint)renderTargetBitmap.PixelWidth,
                                      (uint)renderTargetBitmap.PixelHeight,
                                      96, 96, pixelBuffer);
                await encoder.FlushAsync();
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await OdtworzInformacjeGlosowa(ConfirmationMessage);

            var result = await ShowConfirmationDialog();

            if (result == ContentDialogResult.Primary)
            {
                Application.Current.Exit();
            }
        }
    }
}


