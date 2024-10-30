using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CheckApp
{
    public partial class SecondWindow : Window
    {
        private TextBlock? totalText;
        private TextBlock? rpmText;
        private TextBlock? connectionText;
        private TextBlock? configText;

        public SecondWindow()
        {
            InitializeComponent();
            this.Opened += OnWindowOpened;  // Se asegura de que los controles están cargados
            this.SizeChanged += OnWindowSizeChanged;  // Maneja los cambios de tamaño
        }

        private void AdjustFontSize(TextBlock? textBlock, double scaleFactor = 1)
        {
            if (textBlock == null || string.IsNullOrEmpty(textBlock.Text))
            {
                return;  // Si el control es null o su texto está vacío, no hagas nada
            }

            double windowWidth = this.Bounds.Width;
            int textLength = textBlock.Text.Length + 1;  // Longitud del texto + 1 para evitar divisiones por 0
            double fontSize = (windowWidth / textLength) * scaleFactor;  // Ajuste dinámico del tamaño de la fuente con factor de escala
            textBlock.FontSize = fontSize;  // Cambiar el tamaño de la fuente
        }

        private void OnWindowOpened(object? sender, System.EventArgs e)
        {
            // Obtener referencias a los controles de texto
            totalText = this.FindControl<TextBlock>("TotalText");
            rpmText = this.FindControl<TextBlock>("RPMText");
            connectionText = this.FindControl<TextBlock>("ConnectionText");
            configText = this.FindControl<TextBlock>("ConfigText");

            // Verificar si se encontraron los controles antes de ajustar el tamaño
            if (totalText != null && rpmText != null && connectionText != null && configText != null)
            {
                // Ajustar tamaño inicial para los TextBlocks
                AdjustFontSize(totalText, 1.7);  // Ajuste normal para TotalText
                AdjustFontSize(rpmText, 0.2);  // Reducir tamaño para RPMText
                AdjustFontSize(connectionText, 0.3);  // Reducir tamaño para ConnectionText
                AdjustFontSize(configText, 1.5);  // Reducir tamaño para ConfigText
            }
            else
            {
                // Manejo de error en caso de que algún TextBlock no se encuentre
                System.Console.WriteLine("Uno o más controles de texto no fueron encontrados.");
            }
        }

        private void OnWindowSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            // Ajustar el tamaño de los textos cuando se cambia el tamaño de la ventana
            AdjustFontSize(totalText,1.7);  // Tamaño normal para TotalText
            AdjustFontSize(rpmText, 0.2);  // Reducir tamaño para RPMText
            AdjustFontSize(connectionText, 0.3);  // Reducir tamaño para ConnectionText
            AdjustFontSize(configText, 1.5);  // Reducir tamaño para ConfigText
        }
    }
}
