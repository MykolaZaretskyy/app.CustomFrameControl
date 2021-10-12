using Xamarin.Forms;

namespace CustomFrameApp.Controls
{
    public class Border : Frame
    {
        private const int DefaultPadding = 0;

        public static readonly BindableProperty BorderThicknessProperty = BindableProperty.Create(nameof(BorderThickness), typeof(Thickness), typeof(Border), default(Thickness));

        public Border()
        {
            Padding = DefaultPadding;
        }
        
        public Thickness BorderThickness
        {
            get => (Thickness)GetValue(BorderThicknessProperty);
            set => SetValue(BorderThicknessProperty, value);
        }
    }
}