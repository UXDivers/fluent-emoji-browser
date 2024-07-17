namespace FluentEmojiBrowser;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        BindingContext = new MainViewModel();
    }

    private void OnEmojiTapped(object sender, TappedEventArgs e)
    {
        var unicode = (sender as View)?.BindingContext as string;

        DisplayAlert("Emoji Unicode", unicode, "Ok");
    }
}
