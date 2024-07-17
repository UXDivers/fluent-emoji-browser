namespace FluentEmojiBrowser
{
    public enum FluentEmojiStyle
    {
        Color,
        Flat,
        HighContrast,
        ThreeD
    }
    
    public class FluentEmoji : Grid
    {
        public static readonly BindableProperty UnicodeProperty = BindableProperty.Create(
            nameof(Unicode), 
            typeof(string), 
            typeof(FluentEmoji), 
            null, 
            propertyChanged: (b, o, n) => ((FluentEmoji)b).OnSourceChanged());

        public string Unicode
        {
            get => (string)GetValue(UnicodeProperty);
            set => SetValue(UnicodeProperty, value);
        }
        
        public static readonly BindableProperty EmojiStyleProperty = BindableProperty.Create(
            nameof(EmojiStyle), 
            typeof(FluentEmojiStyle), 
            typeof(FluentEmoji), 
            FluentEmojiStyle.Color, 
            propertyChanged: (b, o, n) => ((FluentEmoji)b).OnSourceChanged());

        public FluentEmojiStyle EmojiStyle
        {
            get => (FluentEmojiStyle)GetValue(EmojiStyleProperty);
            set => SetValue(EmojiStyleProperty, value);
        }

        public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(
            nameof(Placeholder),
            typeof(ImageSource),
            typeof(FluentEmoji),
            null);

        public ImageSource Placeholder
        {
            get => (ImageSource)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly BindableProperty ErrorPlaceholderProperty = BindableProperty.Create(
             nameof(ErrorPlaceholder),
             typeof(ImageSource),
             typeof(FluentEmoji),
             null);

        public ImageSource ErrorPlaceholder
        {
            get => (ImageSource)GetValue(ErrorPlaceholderProperty);
            set => SetValue(ErrorPlaceholderProperty, value);
        }

        private readonly Image _pngImage;
        private readonly SvgImage _svgImage;
        
        public FluentEmoji()
        {
            _pngImage = new Image
            {
                Aspect = Aspect.AspectFit
            };

            _svgImage = new SvgImage
            {
                UseCache = true
            };

            _svgImage.Error += OnSvgError;
            _svgImage.Ready += OnSvgReady;
        }

        private void OnSvgReady(object sender, EventArgs e)
        {
            Children.Remove(_pngImage);
        }

        private void OnSvgError(object sender, SvgImageErrorEventArgs e)
        {
            if (ErrorPlaceholder != null)
            {
                _pngImage.Source = ErrorPlaceholder;
            }
            else
            {
                Children.Remove(_pngImage);
            }
        }

        private void OnSourceChanged()
        {
            _svgImage.Source = null;

            if (string.IsNullOrEmpty(Unicode))
            {
                _pngImage.Source = null;
                Children.Clear();
                return;
            }

            _pngImage.Source = Placeholder;
            if (_pngImage.Parent == null)
            {
                Children.Insert(0, _pngImage);
            }

            if (EmojiStyle == FluentEmojiStyle.ThreeD)
            {
                _pngImage.Source = new UriImageSource
                {
                    Uri = new Uri($"https://cdn.jsdelivr.net/gh/UXDivers/emojis@main/Fluent/3d/{Unicode}.png"),
                    CacheValidity = TimeSpan.MaxValue,
                    CachingEnabled = true
                };

                Children.Remove(_svgImage);
            }
            else
            {
                string style = EmojiStyle switch
                {
                    FluentEmojiStyle.Color => "color",
                    FluentEmojiStyle.Flat => "flat",
                    FluentEmojiStyle.HighContrast => "high_contrast",
                    _ => throw new ArgumentException("Invalid style")
                };

                _svgImage.Source = new UriImageSource
                {
                    Uri = new Uri($"https://cdn.jsdelivr.net/gh/UXDivers/emojis@main/Fluent/{style}/{Unicode}.svg"),
                    CacheValidity = TimeSpan.MaxValue,
                    CachingEnabled = true
                };

                if (_svgImage.Parent == null)
                {
                    Children.Add(_svgImage);
                }
            }
        }
    }
}
