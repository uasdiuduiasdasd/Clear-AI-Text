using ClearAIText.App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace ClearAIText.App.Views;

public sealed partial class AboutPage : Page
{
    private readonly LocalizationService _localizationService;

    public AboutPage()
    {
        InitializeComponent();

        var app = (App)Application.Current;
        _localizationService = app.LocalizationService;

        ApplyLocalization();
        _localizationService.LanguageChanged += () => DispatcherQueue.TryEnqueue(ApplyLocalization);
    }

    public void ApplyLocalization()
    {
        AboutTitleText.Text = _localizationService["AboutTitle"];
        AboutVersionText.Text = _localizationService["AboutVersionFormat"];
        AboutSubtitleText.Text = _localizationService["AboutSubtitle"];
        TechStackHeader.Text = _localizationService["AboutTechStackHeader"];

        Feature1Title.Text = _localizationService["AboutSecurityTitle"];
        Feature1Desc.Text = _localizationService["AboutSecurityDesc"];

        Feature2Title.Text = _localizationService["AboutPerfTitle"];
        Feature2Desc.Text = _localizationService["AboutPerfDesc"];

        Feature3Title.Text = _localizationService["AboutEngineTitle"];
        Feature3Desc.Text = _localizationService["AboutEngineDesc"];
    }
}
