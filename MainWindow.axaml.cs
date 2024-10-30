using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace CheckApp
{
    public partial class MainWindow : Window
    {
        private bool isConnected = false;
        private bool isFirstReading = true;
        private int decimalValue = 0;
        private int rpmValue = 0;
        private int baseValue = 0; // Valor base para el reinicio
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private int PollingInterval = 5; // 1 second
        private int previousDecimalValue = 0;
        private int turns = 0;
        private int pulses = 10000; // Pulsos por defecto
        private double diameter = 16;
        private double scale = 1; // Escala por defecto
        private string unit = "M"; // Unidad por defecto
        private string ipAddress = "192.168.1.250"; // IP por defecto
        private bool _isMinimized = false;
        private HttpClient _httpClient = new HttpClient();
        private double totalDisplacement = 0;

        // Constantes para SetWindowPos (Windows)
        private const int HWND_TOPMOST = -1;
        private const int HWND_NOTOPMOST = -2;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;

        // Declaración de SetWindowPos (Windows)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        // Declaración de FindWindow (Windows)
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Declaración de NSWindow y métodos de Cocoa (macOS)
        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr objc_getClass(string className);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr sel_registerName(string selectorName);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, bool arg);

        [DllImport("/System/Library/Frameworks/AppKit.framework/AppKit")]
        private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, int arg);

        private string configFilePath = "config.json"; // Ruta del archivo de configuración
        private  SecondWindow _secondWindow;
        public MainWindow(SecondWindow secondWindow)
        {
            InitializeComponent();
            _secondWindow = secondWindow;
            LoadConfig(); // Cargar configuración al iniciar
            this.Opened += OnOpened;
            this.SizeChanged += OnSizeChanged;
            this.Closing += OnClosing; // Asegúrate de que OnClosing coincida con la firma correcta
            UpdateConfigSummary();
            
        }

        private void SaveConfig()
        {
            var config = new AppConfig
            {
                PollingInterval = PollingInterval,
                Pulses = pulses,
                Diameter = diameter,
                Scale = scale,
                Unit = unit,
                IpAddress = ipAddress,
                BackgroundColor = this.Background.ToString(),
                TextColor = ColorModalText.Foreground.ToString() 
            };

            string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFilePath, json);
        }

    private void LoadConfig()
{
    if (File.Exists(configFilePath))
    {
        string json = File.ReadAllText(configFilePath);
        var config = JsonSerializer.Deserialize<AppConfig>(json);

        if (config != null)
        {
            // Cargar la configuración de los parámetros
            PollingInterval = config.PollingInterval;
            pulses = config.Pulses;
            diameter = config.Diameter;
            scale = config.Scale;
            unit = config.Unit;
            ipAddress = config.IpAddress;

            // Cargar el color de fondo y aplicarlo a la ventana principal y a los modales
            var backgroundColorBrush = new SolidColorBrush(Color.Parse(config.BackgroundColor));
            this.Background = backgroundColorBrush;  // Cambiar el fondo de la ventana principal
            _secondWindow.Background= backgroundColorBrush;
            // Aplicar el color de fondo a los modales
            ColorModalBorder.Background = backgroundColorBrush;
            ChangeIpModalBorder.Background = backgroundColorBrush;
            PulsesModalBorder.Background = backgroundColorBrush;
            DiameterModalBorder.Background = backgroundColorBrush;
            UnitModalBorder.Background = backgroundColorBrush;
            ScaleModalBorder.Background = backgroundColorBrush;
            IntervalModalBorder.Background = backgroundColorBrush;
            VisualizarModalBorder.Background = backgroundColorBrush;
            ConfirmResetModalBorder.Background = backgroundColorBrush;

            // Cargar el color del texto y aplicarlo a los textos en los modales
            var textColorBrush = new SolidColorBrush(Color.Parse(config.TextColor));
            ColorModalText.Foreground = textColorBrush;
            ColorTextLabel.Foreground = textColorBrush;
            ChangeIpModalText.Foreground = textColorBrush;
            PulsesModalText.Foreground = textColorBrush;
            DiameterModalText.Foreground = textColorBrush;
            UnitModalText.Foreground = textColorBrush;
            ScaleModalText.Foreground = textColorBrush;
            IntervalModalText.Foreground = textColorBrush;
            DecimalValueText2.Foreground = textColorBrush;
            ConfirmResetModalText.Foreground = textColorBrush;
            RPMText.Foreground = textColorBrush;
            ConnectionText.Foreground = textColorBrush;
            TotalText.Foreground = textColorBrush;
            ConfigText.Foreground = textColorBrush;
             _secondWindow.RPMText.Foreground = textColorBrush;
            _secondWindow.ConnectionText.Foreground = textColorBrush;
            _secondWindow.TotalText.Foreground = textColorBrush;
            _secondWindow.ConfigText.Foreground = textColorBrush;
        }
    }
}


        private void OnOpened(object? sender, EventArgs e)
        {
            var screen = this.Screens?.Primary;
            if (screen != null)
            {
                this.Width = screen.Bounds.Width * 0.6;
                this.Height = screen.Bounds.Height * 0.6;
            }
        }

        private void OnCloseButtonClick(object? sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OnMinimizeClick(object? sender, RoutedEventArgs e)
        {
            _isMinimized = true;
            UpdateUIForState();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MakeWindowTopMost(true);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MakeWindowTopMostMacOS(true);
            }
        }

        private void OnMaximizeClick(object? sender, RoutedEventArgs e)
        {
            _isMinimized = false;
            UpdateUIForState();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                MakeWindowTopMost(false);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                MakeWindowTopMostMacOS(false);
            }
        }

        private void OnResetClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("ConfirmResetModal");
        }

        private void OnChangeIpClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("ChangeIpModal");
        }

        private void OnPulsesClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("PulsesModal");
            PulsesInput.Text = pulses.ToString(); // Mostrar pulsos por defecto
        }

        private void OnDiameterClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("DiameterModal");
            DiameterInput.Text = diameter.ToString();
        }

        private void OnUnitClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("UnitModal");
            UnitSelector.SelectedIndex = 1; // Seleccionar "M" (Metros) por defecto
        }

        private void OnScaleClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("ScaleModal");
            ScaleInput.Text = scale.ToString(); // Mostrar escala por defecto
        }

        private void OnAcceptIntervalClick(object? sender, RoutedEventArgs e)
        {
            if (int.TryParse(IntervalInput.Text, out int parsedInterval))
            {
                PollingInterval = parsedInterval;
            }
            else
            {
                ShowStatusMessage("Valores de entrada inválidos.", Avalonia.Media.Brushes.Red);
                return;
            }

            SaveConfig(); // Guardar configuración
            HideAllModals();
            UpdateConfigSummary();
        }

        private void OnIntervalClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("IntervalModal");
            IntervalInput.Text = PollingInterval.ToString(); // Mostrar intervalo por defecto
        }

        private void ShowModal(string modalName)
        {
            HideAllModals();
            var modal = this.FindControl<Canvas>(modalName);
            if (modal != null)
            {
                modal.IsVisible = true;
                CenterModal(modal);
            }
        }

        private void HideAllModals()
        {
            ChangeIpModal.IsVisible = false;
            PulsesModal.IsVisible = false;
            DiameterModal.IsVisible = false;
            UnitModal.IsVisible = false;
            IntervalModal.IsVisible = false;
            ScaleModal.IsVisible = false;
            VisualizarModal.IsVisible = false;
            ConfirmResetModal.IsVisible = false;
            ColorModal.IsVisible=false;
            
        }

        private void OnCloseModalClick(object? sender, RoutedEventArgs e)
        {
            HideAllModals();
        }

private async void OnConnectButtonClick(object? sender, RoutedEventArgs e)
{
    ShowStatusMessage("Cargando...", Avalonia.Media.Brushes.Orange);

    if (ChangeIpModal != null)
    {
        ChangeIpModal.IsVisible = false;
    }

    ipAddress = IpInput.Text ?? ipAddress;
    string pulsesText = PulsesInput.Text ?? string.Empty;
    string diameterText = DiameterInput.Text ?? string.Empty;

    string url = $"http://{ipAddress}/path/to/api";

    var requestBody = new
    {
        code = "request",
        cid = 4711,
        adr = "/iolinkmaster/port[1]/iolinkdevice/pdin/getdata",
    };

    string json = JsonSerializer.Serialize(requestBody);
    var content = new StringContent(json, Encoding.UTF8, "application/json");

    try
    {
        HttpResponseMessage response = await _httpClient.PostAsync(url, content);
        if (response.IsSuccessStatusCode)
        {
            string responseBody = await response.Content.ReadAsStringAsync();
            ProcessResponse(responseBody);

            if (!isConnected)
            {
                isFirstReading = true; // Reiniciar la bandera para la primera lectura
                var response2 = JsonSerializer.Deserialize<ResponseData>(responseBody);
                if (response2 != null)
                {
                    string hexValue = response2.data?.value ?? string.Empty;
                    if (hexValue.Length >= 4)
                    {
                        string firstFourHex = hexValue.Substring(0, 4);
                        rpmValue = Convert.ToInt32(firstFourHex, 16);

                        string lastFourHex = hexValue.Substring(hexValue.Length - 4);
                        string binaryValue = Convert.ToString(Convert.ToInt32(lastFourHex, 16), 2).PadLeft(16, '0');

                        string trimmedBinaryValue = binaryValue.Substring(0, binaryValue.Length - 2);
                        int rawDecimalValue = Convert.ToInt32(trimmedBinaryValue, 2);

                        baseValue = rawDecimalValue;
                        previousDecimalValue = 0;
                        turns = 0;
                        isFirstReading = false;
                    }
                }
            }

            StartPolling(url, json);
            isConnected = true;
            ShowStatusMessage("Conectado", Avalonia.Media.Brushes.Green);
        }
        else
        {
            ShowStatusMessage("No se pudo establecer la conexión. Código de estado: " + response.StatusCode, Avalonia.Media.Brushes.Red);
        }
    }
    catch (Exception ex)
    {
        ShowStatusMessage($"Error: {ex.Message}", Avalonia.Media.Brushes.Red);
    }
}

private void StartPolling(string url, string json)
{
    _cancellationTokenSource = new CancellationTokenSource();
    var token = _cancellationTokenSource.Token;

    Task.Run(async () =>
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync(url, content);
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Dispatcher.UIThread.Post(() => ProcessResponse(responseBody));
                }
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => ShowStatusMessage($"Error: {ex.Message}", Avalonia.Media.Brushes.Red));
            }
            await Task.Delay(PollingInterval, token);
        }
    }, token);
}

        private void StopPolling()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void ProcessResponse(string responseBody)
        {
            try
            {
                var response = JsonSerializer.Deserialize<ResponseData>(responseBody);
                if (response != null)
                {
                    string hexValue = response.data?.value ?? string.Empty;
                    if (hexValue.Length >= 4)
                    {
                        string firstFourHex = hexValue.Substring(0, 4);
                        rpmValue = Convert.ToInt32(firstFourHex, 16);
                         // Check if rpmValue is near the upper limit, indicating reverse direction
                if (rpmValue > 32767)
                {
                    rpmValue = rpmValue - 65536; // Convert to negative value
                }

                string rpmText = $"{rpmValue:F2} RPM";
                RpmStatus.Text = rpmText;
                        string lastFourHex = hexValue.Substring(hexValue.Length - 4);
                        string binaryValue = Convert.ToString(Convert.ToInt32(lastFourHex, 16), 2).PadLeft(16, '0');

                        string trimmedBinaryValue = binaryValue.Substring(0, binaryValue.Length - 2);
                        int rawDecimalValue = Convert.ToInt32(trimmedBinaryValue, 2);
                        DecimalValueText.Text = rawDecimalValue.ToString();

                        // Asegurar que baseValue no exceda pulses
                        baseValue %= pulses;

                        decimalValue = (rawDecimalValue - baseValue + pulses) % pulses;

                        if (decimalValue < 0)
                        {
                            decimalValue += pulses;
                        }

                        if (decimalValue < previousDecimalValue && previousDecimalValue - decimalValue > pulses / 2)
                        {
                            turns++;
                        }
                        else if (decimalValue > previousDecimalValue && decimalValue - previousDecimalValue > pulses / 2)
                        {
                            turns--;
                        }

                        previousDecimalValue = decimalValue;

                        double perimeter = Math.PI * diameter;
                        double displacementByTurns = perimeter * turns;
                        double fractionOfPulses = decimalValue / (double)pulses;
                        double additionalDisplacement = fractionOfPulses * perimeter;
                        double totalDisplacement = displacementByTurns + additionalDisplacement;

                        totalDisplacement = ConvertDistance(totalDisplacement, unit);
                        totalDisplacement *= scale;

                        string displayText = $"{totalDisplacement:F2} {unit.ToLower()}";
                        if (_secondWindow != null)
                        {
                            _secondWindow.TotalDisplacement.Text = displayText;
                            _secondWindow.RpmStatus.Text = rpmText;
                        }
                        AdjustFontSize(displayText);

                        TotalDisplacement.Text = displayText;
                         
                    }
                    else
                    {
                        ShowStatusMessage("El valor hexadecimal es demasiado corto.", Avalonia.Media.Brushes.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                ShowStatusMessage($"Error al procesar la respuesta: {ex.Message}", Avalonia.Media.Brushes.Red);
            }
        }

        private void AdjustFontSize(string text)
        {
            int length = text.Length;
            double fontSize = 157; // Tamaño de fuente por defecto

            if (_isMinimized)
            {
                if (length <= 5)
                {
                    fontSize = 63;
                }
                else if (length <= 10)
                {
                    fontSize = 51;
                }
                else if (length <= 15)
                {
                    fontSize = 39;
                }
                else
                {
                    fontSize = 33;
                }
            }
            else // Maximizado
            {
                if (length <= 5)
                {
                    fontSize = 137; // Incrementar el tamaño de fuente en 50
                }
                else if (length <= 10)
                {
                    fontSize = 125; // Incrementar el tamaño de fuente en 50
                }
                else if (length <= 15)
                {
                    fontSize = 113; // Incrementar el tamaño de fuente en 50
                }
                else
                {
                    fontSize = 101; // Incrementar el tamaño de fuente en 50
                }
            }

            TotalDisplacement.FontSize = fontSize;
        }

        private void UpdateUIForState()
        {
            if (_isMinimized)
            {
                this.Width = 300;
                this.Height = 200;
                this.MinWidth = 300;
                this.MinHeight = 200;
                this.MaxWidth = 300;
                this.MaxHeight = 200;
                TotalDisplacement.Width = 250;
                MainMenu.FindControl<MenuItem>("ActionsMenuItem").IsVisible = false;
                MainMenu.FindControl<MenuItem>("ConfigMenuItem").IsVisible = false;
                MainMenu.FindControl<MenuItem>("MinimizeMenuItem").IsVisible = false;
                MainMenu.FindControl<MenuItem>("MaximizeMenuItem").IsVisible = true;
                MainMenu.FindControl<MenuItem>("VisualizarMenuItem").IsVisible = false;
                ConfigSummary.IsVisible = false;
            }
            else
            {
                this.Width = 800;
                this.Height = 600;
                this.MinWidth = 800;
                this.MinHeight = 600;
                this.MaxWidth = 800;
                this.MaxHeight = 600;
                TotalDisplacement.Width = 700;
                MainMenu.FindControl<MenuItem>("ActionsMenuItem").IsVisible = true;
                MainMenu.FindControl<MenuItem>("ConfigMenuItem").IsVisible = true;
                MainMenu.FindControl<MenuItem>("MinimizeMenuItem").IsVisible = true;
                MainMenu.FindControl<MenuItem>("MaximizeMenuItem").IsVisible = false;
                MainMenu.FindControl<MenuItem>("VisualizarMenuItem").IsVisible = true;
                ConfigSummary.IsVisible = true;
                UpdateConfigSummary(); // Asegurarse de que el resumen de configuración se actualice
            }

            // Ajustar el tamaño de la fuente del texto
            AdjustFontSize(TotalDisplacement.Text);

            // Forzar la actualización de la UI
            Dispatcher.UIThread.Post(() =>
            {
                this.InvalidateVisual();
                this.InvalidateMeasure();
                this.InvalidateArrange();
            });
        }

        private void MakeWindowTopMost(bool topMost)
        {
            var windowHandle = FindWindow(null, this.Title);
            if (windowHandle != IntPtr.Zero)
            {
                SetWindowPos(windowHandle, topMost ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
        }

        private void MakeWindowTopMostMacOS(bool topMost)
        {
            var nsWindow = GetNSWindow();
            if (nsWindow != IntPtr.Zero)
            {
                var selector = sel_registerName("setLevel:");
                int level = topMost ? 10 : 0; // kCGFloatingWindowLevel = 10, kCGNormalWindowLevel = 0
                objc_msgSend(nsWindow, selector, level);
            }
        }

        private IntPtr GetNSWindow()
        {
            var windowClass = objc_getClass("NSApplication");
            var sharedAppSelector = sel_registerName("sharedApplication");
            var sharedApp = objc_msgSend(windowClass, sharedAppSelector);
            var mainWindowSelector = sel_registerName("mainWindow");
            var nsWindow = objc_msgSend(sharedApp, mainWindowSelector);
            return nsWindow;
        }

        private double ConvertDistance(double distanceInCm, string unit)
        {
            return unit switch
            {
                "M" => distanceInCm / 100,
                "MM" => distanceInCm * 10,
                _ => distanceInCm // Default is CM
            };
        }

        private void ShowStatusMessage(string message, Avalonia.Media.IBrush color)
        {
            if (ConnectionStatus != null)
            {
                ConnectionStatus.Text = message;
                ConnectionStatus.Foreground = color;
                _secondWindow.ConnectionStatus.Foreground = color;
                if (_secondWindow != null)
                {
                    _secondWindow.ConnectionStatus.Text = message;
                    _secondWindow.Foreground = color;
                }
            }
        }

        private void OnAcceptPulsesClick(object? sender, RoutedEventArgs e)
        {
            if (int.TryParse(PulsesInput.Text, out int parsedPulses))
            {
                pulses = parsedPulses;
            }
            else
            {
                ShowStatusMessage("Valores de entrada inválidos.", Avalonia.Media.Brushes.Red);
                return;
            }

            SaveConfig(); // Guardar configuración
            HideAllModals();
            UpdateConfigSummary();
        }

        private void OnAcceptDiameterClick(object? sender, RoutedEventArgs e)
        {
            if (double.TryParse(DiameterInput.Text, out double parsedDiameter))
            {
                diameter = parsedDiameter;
            }
            else
            {
                ShowStatusMessage("Valores de entrada inválidos.", Avalonia.Media.Brushes.Red);
                return;
            }

            SaveConfig(); // Guardar configuración
            HideAllModals();
            UpdateConfigSummary();
        }

        private void OnAcceptUnitClick(object? sender, RoutedEventArgs e)
        {
            var selectedUnit = UnitSelector.SelectedItem as ComboBoxItem;
            if (selectedUnit != null)
            {
                unit = selectedUnit.Content.ToString();
            }
            else
            {
                ShowStatusMessage("Selección de unidad inválida.", Avalonia.Media.Brushes.Red);
                return;
            }

            SaveConfig(); // Guardar configuración
            HideAllModals();
            UpdateConfigSummary();
        }

        private void OnAcceptScaleClick(object? sender, RoutedEventArgs e)
        {
            if (double.TryParse(ScaleInput.Text, out double parsedScale))
            {
                scale = parsedScale;
            }
            else
            {
                ShowStatusMessage("Valores de entrada inválidos.", Avalonia.Media.Brushes.Red);
                return;
            }

            SaveConfig(); // Guardar configuración
            HideAllModals();
            UpdateConfigSummary();
        }

        private void UpdateConfigSummary()
        {
            if (ConfigSummary != null)
            {
                  Console.WriteLine("Hola");
                ConfigSummary.Text = $"IP: {ipAddress}, Diametro: {diameter}, Pulsos: {pulses}, Unidad: {unit}, Escala: {scale}";
                ConfigSummary.InvalidateVisual(); // Forzar actualización visual
                if (_secondWindow != null)
                {
                    Console.WriteLine("Hola");
                    _secondWindow.ConfigSummary.Text = $"Segunda pantalla: IP: {ipAddress}, Diametro: {diameter}, Pulsos: {pulses}, Unidad: {unit}, Escala: {scale}";
                }
            }
        }

        private void VisualizarClick(object? sender, RoutedEventArgs e)
        {
            ShowModal("VisualizarModal");
            DecimalValueText.Text = decimalValue.ToString();
        }

        private void OnConfirmResetClick(object? sender, RoutedEventArgs e)
        {
            ResetDecimalValue();
            HideAllModals();
        }

        private void OnSizeChanged(object? sender, EventArgs e)
        {
            CenterModals();
        }

        private void CenterModals()
        {
            CenterModal(ChangeIpModal);
            CenterModal(PulsesModal);
            CenterModal(DiameterModal);
            CenterModal(UnitModal);
            CenterModal(ScaleModal);
            CenterModal(IntervalModal);
            CenterModal(VisualizarModal);
            CenterModal(ConfirmResetModal);
        }

        private void CenterModal(Canvas modal)
        {
            if (modal != null && modal.Children.Count > 0)
            {
                Canvas.SetLeft(modal.Children[0], 200);
                Canvas.SetTop(modal.Children[0], 100);
            }
        }

        private void ResetDecimalValue()
        {
            baseValue = (baseValue + decimalValue) % pulses;
            previousDecimalValue = 0;
            turns = 0;
            totalDisplacement = 0;

            // Actualizar el texto en la interfaz de usuario
            //DecimalValueText.Text = "0";
            TotalDisplacement.Text = "0 " + unit.ToLower();
        }

        private void OnClosing(object? sender, WindowClosingEventArgs e)
        {
            // Manejar el evento de cierre aquí si es necesario
        }

        private void IpInput_GotFocus(object sender, GotFocusEventArgs e)
        {
            var textBox = sender as TextBox;
            Console.WriteLine(textBox);
            if (textBox != null)
            {
                textBox.Background = new SolidColorBrush(Colors.White);
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }

        private void IpInput_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox != null)
            {
                textBox.Background = new SolidColorBrush(Colors.White);
                textBox.Foreground = new SolidColorBrush(Colors.Black);
            }
        }
          private void OnChangeColorClick(object? sender, RoutedEventArgs e)
            {
                ShowModal("ColorModal");
                ColorInput.Text = Background.ToString(); // Mostrar el color actual
                TextColorInput.Text = ColorModalText.Foreground.ToString();
            }private void OnAcceptColorClick(object? sender, RoutedEventArgs e)
{
    string backgroundColorHex = ColorInput.Text;
    string textColorHex = TextColorInput.Text;

    // Validar si ambos colores ingresados son hexadecimales válidos
    if ((backgroundColorHex.StartsWith("#") && (backgroundColorHex.Length == 7 || backgroundColorHex.Length == 9)) &&
        (textColorHex.StartsWith("#") && (textColorHex.Length == 7 || textColorHex.Length == 9)))
    {
        try
        {
            // Cambiar el color de fondo
            var backgroundColor = Color.Parse(backgroundColorHex);
            var backgroundBrush = new SolidColorBrush(backgroundColor);
            this.Background = backgroundBrush; // Cambiar el fondo de la ventana principal
            _secondWindow.Background=backgroundBrush;

            // Cambiar el color de texto
            var textColor = Color.Parse(textColorHex);
            var textBrush = new SolidColorBrush(textColor);

            // Aplicar los cambios de color a los modales

            // ColorModal
            ColorModalBorder.Background = backgroundBrush;
            ColorModalText.Foreground = textBrush;
            ColorTextLabel.Foreground = textBrush;

            // ChangeIpModal
            ChangeIpModalBorder.Background = backgroundBrush;
            ChangeIpModalText.Foreground = textBrush;

            // PulsesModal
            PulsesModalBorder.Background = backgroundBrush;
            PulsesModalText.Foreground = textBrush;

            // DiameterModal
            DiameterModalBorder.Background = backgroundBrush;
            DiameterModalText.Foreground = textBrush;

            // UnitModal
            UnitModalBorder.Background = backgroundBrush;
            UnitModalText.Foreground = textBrush;

            // ScaleModal
            ScaleModalBorder.Background = backgroundBrush;
            ScaleModalText.Foreground = textBrush;

            // IntervalModal
            IntervalModalBorder.Background = backgroundBrush;
            IntervalModalText.Foreground = textBrush;

            // VisualizarModal
            VisualizarModalBorder.Background = backgroundBrush;
            DecimalValueText2.Foreground = textBrush;

            // ConfirmResetModal
            ConfirmResetModalBorder.Background = backgroundBrush;
            ConfirmResetModalText.Foreground = textBrush;

            RPMText.Foreground = textBrush;
            ConnectionText.Foreground = textBrush;
            TotalText.Foreground = textBrush;
            ConfigText.Foreground = textBrush;
           
            _secondWindow.RPMText.Foreground = textBrush;
            _secondWindow.ConnectionText.Foreground = textBrush;
            _secondWindow.TotalText.Foreground = textBrush;
            _secondWindow.ConfigText.Foreground = textBrush;
            

            // Guardar los colores en la configuración (opcional)
            SaveConfig();
            UpdateConfigSummary();
            ShowStatusMessage("", Brushes.Red);
        }
        catch
        {
            ShowStatusMessage("Color inválido.", Brushes.Red);
        }
    }
    else
    {
        ShowStatusMessage("Color inválido.", Brushes.Red);
    }

    HideAllModals();
}



    }
  

    public class AppConfig
    {
        public int PollingInterval { get; set; }
        public int Pulses { get; set; }
        public double Diameter { get; set; }
        public double Scale { get; set; }
        public string Unit { get; set; }
        public string IpAddress { get; set; }
        public string BackgroundColor { get; set; } = "#34495E";
         public string TextColor { get; set; } = "#FFFFFF"; 
    }

    public class ResponseData
    {
        public Data data { get; set; } = new Data();
    }

    public class Data
    {
        public string value { get; set; } = string.Empty;
    }
}
