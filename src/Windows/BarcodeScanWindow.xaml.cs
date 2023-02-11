using BarcodeReaderTool;
using System;
using System.Windows;

namespace COMPASS.Windows
{
    //based on https://github.com/FrancescoBonizzi/WebcamControl-WPF-With-OpenCV
    public partial class BarcodeScanWindow : Window
    {


        private WebcamStreaming _webcamStreaming;

        public BarcodeScanWindow()
        {
            InitializeComponent();
            StartScanning();
        }

        public string DecodedString = "";

        private async void StartScanning()
        {
            cameraLoading.Visibility = Visibility.Visible;
            webcamPreview.Visibility = Visibility.Hidden;

            var selectedCameraDeviceId = 0;
            if (_webcamStreaming == null || _webcamStreaming.CameraDeviceId != selectedCameraDeviceId)
            {
                _webcamStreaming?.Dispose();
                _webcamStreaming = new WebcamStreaming(
                    imageControlForRendering: webcamPreview,
                    frameWidth: 300,
                    frameHeight: 300,
                    cameraDeviceId: selectedCameraDeviceId);
                _webcamStreaming.OnQRCodeRead += _webcamStreaming_OnQRCodeRead;
            }

            try
            {
                await _webcamStreaming.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            cameraLoading.Visibility = Visibility.Collapsed;
            webcamPreview.Visibility = Visibility.Visible;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) => _webcamStreaming?.Dispose();

        private async void _webcamStreaming_OnQRCodeRead(object sender, EventArgs e)
        {
            var qrCodeData = (e as QRCodeReadEventArgs).QRCodeData;
            if (!string.IsNullOrWhiteSpace(qrCodeData) && IsValidISBN(qrCodeData))
            {
                DecodedString = qrCodeData;
                await _webcamStreaming.Stop();
                Dispatcher.Invoke(() =>
                {
                    DialogResult = true;
                    Close();
                });

            }
        }

        private static bool IsValidISBN(string isbn)
        {
            // length must be 10
            int n = isbn.Length;
            int sum = 0;

            switch (n)
            {
                case 10:
                    //https://www.geeksforgeeks.org/program-check-isbn/

                    // Computing weighted sum of first 9 digits
                    for (int i = 0; i < 9; i++)
                    {
                        int digit = isbn[i] - '0';
                        if (0 > digit || 9 < digit)
                            return false;
                        sum += digit * (10 - i);
                    }

                    // Checking last digit.
                    char last = isbn[9];
                    if (last != 'X' && (last < '0' || last > '9'))
                        return false;

                    // If last digit is 'X', add 10 to sum, else add its value.
                    sum += (last == 'X') ? 10 : (last - '0');

                    // Return true if weighted sum of digits is divisible by 11.
                    return sum % 11 == 0;

                case 13:
                    for (int i = 0; i < 13; i++)
                    {
                        int digit = isbn[i] - '0';
                        if (0 > digit || 9 < digit)
                            return false;
                        sum += digit * (1 + (2 * (i % 2)));
                    }
                    // Return true if weighted sum of digits is divisible by 10.
                    return sum % 10 == 0;

                default:
                    return false;
            }
        }
    }
}
