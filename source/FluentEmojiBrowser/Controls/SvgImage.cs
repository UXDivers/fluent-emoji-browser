using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace FluentEmojiBrowser
{
    public class SvgImage : SKCanvasView
    {
        public event EventHandler Ready;
        public event EventHandler Loading;
        public event EventHandler<SvgImageErrorEventArgs> Error;

        public static readonly BindableProperty SourceProperty = BindableProperty.Create(
            nameof(Source),
            typeof(ImageSource),
            typeof(SvgImage),
            null,
            propertyChanged: (b, o, n) => ((SvgImage)b).OnSourceChanged());

        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly BindableProperty UseCacheProperty = BindableProperty.Create(
            nameof(UseCache),
            typeof(bool),
            typeof(SvgImage),
            false);

        public bool UseCache
        {
            get => (bool)GetValue(UseCacheProperty);
            set => SetValue(UseCacheProperty, value);
        }

        private readonly object _synchandle = new();
        private CancellationTokenSource _cancellationTokenSource;
        private SKPicture _svgPicture;

        protected static readonly Dictionary<string, SKPicture> ImageCache = new();

        protected virtual SKPicture GetFromCache(string key)
        {
            if (ImageCache.TryGetValue(key, out var cachedPicture))
            {
                return cachedPicture;
            }

            return null;
        }

        protected virtual void StoreInCache(string key, SKPicture picture)
        {
            ImageCache[key] = picture;
        }

        private async void OnSourceChanged()
        {
            var source = Source;

            if (source == null)
            {
                Cancel();

                if (_svgPicture != null)
                {
                    _svgPicture = null;
                    InvalidateSurface();
                }

                return;
            }
            
            Loading?.Invoke(this, EventArgs.Empty);

            SKPicture picture = null;
            Exception error = null;
            try
            {
                picture = await LoadSvgPictureAsync(source, UseCache);
            }
            catch (TaskCanceledException)
            {
                // Image loading cancelled by Source change
            }
            catch (Exception e)
            {
                error = e;
            }

            if (source == Source)
            {
                _svgPicture = picture;

                if (picture != null)
                {
                    Ready?.Invoke(this, EventArgs.Empty);
                }
                else 
                {
                    Error?.Invoke(this, new SvgImageErrorEventArgs($"Error getting image: {source}", error));
                }

                InvalidateSurface();
            }
        }

        private void Cancel()
        {
            lock (_synchandle)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = null;
            }
        }

        private CancellationToken Begin()
        {
            lock (_synchandle)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                return _cancellationTokenSource.Token;
            }
        }

        private async Task<SKPicture> LoadSvgPictureAsync(ImageSource source, bool useCache)
        {
            string cacheKey = source.ToString();
            if (useCache && GetFromCache(cacheKey) is SKPicture cachedPicture)
            {
                return cachedPicture;
            }

            IImageSourceHandler handler = source switch
            {
                FileImageSource fileSource => new FileHandler(fileSource.File),
                UriImageSource uriSource => new UriHandler(uriSource.Uri),
                StreamImageSource streamSource => new StreamHandler(streamSource.Stream),
                _ => throw new InvalidOperationException("Unsupported ImageSource type.")
            };

            using var stream = await handler.GetStreamAsync(Begin());

            if (stream == null)
            {
                return null;
            }

            var svg = SKSvg.CreateFromStream(stream);
            var picture = svg.Picture;

            if (useCache)
            {
                StoreInCache(cacheKey, picture);
            }

            return picture;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);

            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (_svgPicture != null)
            {
                var info = e.Info;
                var pictureBounds = _svgPicture.CullRect;
                var scale = Math.Min(info.Width / pictureBounds.Width, info.Height / pictureBounds.Height);

                canvas.Save();
                canvas.Scale(scale);
                canvas.DrawPicture(_svgPicture);
                canvas.Restore();
            }
        }

        private interface IImageSourceHandler
        {
            public abstract Task<Stream> GetStreamAsync(CancellationToken cancellationToken);
        }

        private class StreamHandler : IImageSourceHandler
        {
            private readonly Func<CancellationToken, Task<Stream>> _getStream;

            public StreamHandler(Func<CancellationToken, Task<Stream>> getStream)
            {
                _getStream = getStream;
            }

            public Task<Stream> GetStreamAsync(CancellationToken cancellationToken)
            {
                return _getStream(cancellationToken);
            }
        }

        private class FileHandler : IImageSourceHandler
        {
            private readonly string _file;

            public FileHandler(string file)
            {
                _file = file;
            }

            public Task<Stream> GetStreamAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(File.OpenRead(_file) as Stream);
            }
        }

        private class UriHandler : IImageSourceHandler
        {
            private readonly Uri _uri;

            public UriHandler(Uri uri)
            {
                _uri = uri;
            }

            public async Task<Stream> GetStreamAsync(CancellationToken cancellationToken)
            {
                var response = await new HttpClient().GetAsync(_uri, cancellationToken);
                return await response.Content.ReadAsStreamAsync();
            }
        }
    }
}