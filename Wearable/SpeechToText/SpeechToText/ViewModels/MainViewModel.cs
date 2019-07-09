//Copyright 2018 Samsung Electronics Co., Ltd
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.

using SpeechToText.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using TextReader.ViewModels;
using Tizen;
using Xamarin.Forms;

namespace SpeechToText.ViewModels
{
    /// <summary>
    /// The application's main view model class.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        #region fields

        /// <summary>
        /// Private backing field for SoundOn property.
        /// </summary>
        private bool _wizardSoundOn = true;

        /// <summary>
        /// An instance of the STT model.
        /// </summary>
        private TextToSpeechModel _ttsModel;

        private string _textToRead = "Xamarin.Forms";
        private string _err;
        private string _errMsg;

        ///// <summary>
        ///// Private backing field for ServiceError property.
        ///// </summary>
        //private SttError _serviceError;

        #endregion

        #region properties

        /// <summary>
        /// The application's navigation class instance.
        /// </summary>
        public INavigation Navigation { get; set; }

        /// <summary>
        /// Command which allows to navigate to another page (specified in command parameter).
        /// </summary>
        public ICommand NavigateCommand { get; private set; }

        /// <summary>
        /// Command which allows to navigate to previous page.
        /// </summary>
        public ICommand NavigateBackCommand { get; private set; }

        /// <summary>
        /// Command which allows to navigate to settings page.
        /// </summary>
        public ICommand NavigateToSettingsCommand { get; private set; }

        /// <summary>
        /// Command which allows to change the language of the STT client.
        /// </summary>
        public ICommand ChangeLanguageCommand { get; private set; }

        /// <summary>
        /// Command which allows to change recognition type of the STT client.
        /// </summary>
        public ICommand ChangeRecognitionTypeCommand { get; private set; }

        /// <summary>
        /// Command which allows to change recognition type of the STT client.
        /// </summary>
        public ICommand ChangeSilenceDetectionCommand { get; private set; }

        /// <summary>
        /// Command which initializes sounds settings wizard (initial value, sounds list data).
        /// </summary>
        public ICommand InitSoundsWizardCommand { get; private set; }

        /// <summary>
        /// Command which saves sound settings.
        /// </summary>
        public ICommand WizardSaveSoundSettingsCommand { get; private set; }

        /// <summary>
        /// Command which starts the recognition.
        /// </summary>
        public ICommand RecognitionStartCommand { get; private set; }

        /// <summary>
        /// Command which pauses the recognition.
        /// </summary>
        public ICommand RecognitionPauseCommand { get; private set; }

        /// <summary>
        /// Command which stops the recognition.
        /// </summary>
        public ICommand RecognitionStopCommand { get; private set; }

        /// <summary>
        /// Command which clears the recognition results.
        /// </summary>
        public ICommand ClearResultCommand { get; private set; }

        /// <summary>
        /// Command which shows message about recognition fail.
        /// The command is injected into view model.
        /// </summary>
        public ICommand RecognitionFailedInfoCommand { get; set; }

        /// <summary>
        /// Command which shows message about service error.
        /// The command is injected into view model.
        /// </summary>
        public ICommand ServiceErrorInfoCommand { get; set; }

        /// <summary>
        /// Command which shows message about unavailability of settings.
        /// The command is injected into view model.
        /// </summary>
        public ICommand SettingsUnavailableInfoCommand { get; set; }

        /// <summary>
        /// Command which shows message about denied privilege (application close).
        /// The command is injected into view model.
        /// </summary>
        public ICommand PrivilegeDeniedInfoCommand { get; set; }

        /// <summary>
        /// Command which handles confirmation of privilege denied dialog.
        /// </summary>
        public ICommand PrivilegeDeniedConfirmedCommand { get; set; }

        public string TextToRead
        {
            get => _textToRead;
            set
            {
                if (_textToRead != value)
                {
                    _textToRead = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Flag indicating if recognition is active.
        /// </summary>
        public bool RecognitionActive => _ttsModel.RecognitionActive;

        /// <summary>
        /// A collection of languages supported by the service.
        ///
        /// The language is specified as an ISO 3166 alpha-2 two letter country-code
        /// followed by ISO 639-1 for the two-letter language code.
        /// </summary>
        public IEnumerable<string> SupportedLanguages => _ttsModel.SupportedLanguages;

        /// <summary>
        /// Current STT client language (code).
        /// </summary>
        public string Language
        {
            get => _ttsModel.Language;
            private set
            {
                _ttsModel.Language = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Flag indicating if STT client sounds (start, end) are turn on (wizard value).
        /// </summary>
        public bool SoundOn => _ttsModel.SoundOn;

        /// <summary>
        /// Wizard value for sound on option.
        /// </summary>
        public bool WizardSoundOn
        {
            get => _wizardSoundOn;
            set => SetProperty(ref _wizardSoundOn, value);
        }


        public string ServiceError
        {
            get => _err;
            set => SetProperty(ref _err, value);
        }

        public string ServiceErrorMessage
        {
            get => _errMsg;
            set => SetProperty(ref _errMsg, value);
        }

        #endregion

        #region methods

        /// <summary>
        /// The view model constructor.
        /// </summary>
        public MainViewModel()
        {
            System.Console.WriteLine($"MainViewModel ctor");
            Console.WriteLine("++++++++++++ MainViewModel");
            try
            {
                _ttsModel = new TextToSpeechModel(Application.Current.Properties);
                _ttsModel.RecognitionActiveStateChanged += SttModelOnRecognitionActiveStateChanged;
                //_ttsModel.RecognitionError += SttModelOnRecognitionError;
                _ttsModel.ServiceError += SttModelOnServiceError;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ex.Message: {ex.Message} \n {ex.StackTrace}");
            }

            InitCommands();

        }

        public async Task InitAsync()
        {
            Console.WriteLine(" +++++++++ MainViewModel init");

            await _ttsModel.InitAsync();
        }

        /// <summary>
        /// Initializes view model's commands.
        /// </summary>
        private void InitCommands()
        {
            NavigateCommand = new Command<Type>(ExecuteNavigate);
            NavigateBackCommand = new Command(ExecuteNavigateBack);
            NavigateToSettingsCommand = new Command<Type>(ExecuteNavigateToSettings);
            ChangeLanguageCommand = new Command<string>(ExecuteChangeLanguage);
            InitSoundsWizardCommand = new Command<Type>(ExecuteInitSoundsWizard);
            WizardSaveSoundSettingsCommand = new Command(ExecuteWizardSaveSoundSettings);
            RecognitionStartCommand = new Command(ExecuteRecognitionStart);
            //RecognitionPauseCommand = new Command(ExecuteRecognitionPause);
            RecognitionStopCommand = new Command(ExecuteRecognitionStop);
            ClearResultCommand = new Command(ExecuteClearResult);
        }

        /// <summary>
        /// Handles execution of navigate command.
        ///
        /// Changes current page to that specified in command parameter.
        /// </summary>
        /// <param name="pageType">Target page class.</param>
        private void ExecuteNavigate(Type pageType)
        {
            Page page = (Page)Activator.CreateInstance(pageType);
            Navigation?.PushModalAsync(page);
        }

        /// <summary>
        /// Handles execution of navigate back command.
        ///
        /// Changes current page to previous one.
        /// </summary>
        private void ExecuteNavigateBack()
        {
            Navigation?.PopModalAsync();
        }

        /// <summary>
        /// Handles execution of settings navigation command.
        ///
        /// Navigates to settings page if there is no ongoing recognition process,
        /// shows proper message (dialog) otherwise.
        /// </summary>
        /// <param name="pageType">Page class to navigate to.</param>
        private void ExecuteNavigateToSettings(Type pageType)
        {
            try
            {
                if (!_ttsModel.Ready)
                {
                    return;
                }

                if (RecognitionActive)
                {
                    SettingsUnavailableInfoCommand?.Execute(null);
                }
                else
                {
                    ExecuteNavigate(pageType);
                }
            }
            catch (Exception e)
            {
                Log.Debug("STT", e.Message + " " + e.GetType());
            }
        }

        /// <summary>
        /// Handles execution of change language command.
        ///
        /// Updates current STT client language.
        /// </summary>
        /// <param name="language">Language to set.</param>
        private void ExecuteChangeLanguage(string language)
        {
            Language = language;

            ExecuteNavigateBack();
        }


        /// <summary>
        /// Handles execution of init sounds wizard command.
        ///
        /// Sets initial values for wizard options and navigates
        /// to a page specified in the command parameter.
        /// </summary>
        /// <param name="pageType">Target page class.</param>
        private void ExecuteInitSoundsWizard(Type pageType)
        {
            WizardSoundOn = SoundOn;

            ExecuteNavigate(pageType);
        }

        /// <summary>
        /// Handles execution of save sound settings command.
        ///
        /// Updates STT model sound settings.
        /// </summary>
        private void ExecuteWizardSaveSoundSettings()
        {
            _ttsModel.SoundOn = WizardSoundOn;
            OnPropertyChanged(nameof(SoundOn));

            ExecuteNavigateBack();
        }

        /// <summary>
        /// Handles execution of recognition start command.
        ///
        /// Calls model to start the recognition.
        /// </summary>
        private void ExecuteRecognitionStart()
        {
            if (!_ttsModel.Ready)
            {
                return;
            }

            if(string.IsNullOrEmpty(TextToRead))
            {
                Console.WriteLine("+++ turn on sound");
                ShowErrorDialog("Service Error", "Please input any text");
                return;
            }

            if(!SoundOn)
            {
                Console.WriteLine("+++ turn on sound");
                ShowErrorDialog("Service Error", "Please turn on sound");
                return;
            }


            Console.WriteLine($"+++++++++++++ ExecuteRecognitionStart Text = {TextToRead}");

            _ttsModel.StartAsync(TextToRead);

            Console.WriteLine($"+++++++++++++ ExecuteRecognitionStart Text = {TextToRead}");
        }

        /// <summary>
        /// Handles execution of recognition pause command.
        ///
        /// Calls model to pause the recognition.
        /// </summary>
        private void ExecuteRecognitionPause()
        {
            if (!_ttsModel.Ready)
            {
                return;
            }

            _ttsModel.Stop();
        }

        /// <summary>
        /// Handles execution of recognition stop command.
        ///
        /// Calls model to stop the recognition.
        /// </summary>
        private void ExecuteRecognitionStop()
        {
            //if (!_ttsModel.Ready)
            //{
            //    return;
            //}

            _ttsModel.Stop();
        }

        /// <summary>
        /// Handles execution of recognition result clear command.
        ///
        /// Calls model to clear the result.
        /// </summary>
        private void ExecuteClearResult()
        {
            if (!_ttsModel.Ready)
            {
                return;
            }

            TextToRead = "";
        }

        /// <summary>
        /// Handles recognition active state change event from the model.
        /// Triggers "RecognitionActive" property change callback.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void SttModelOnRecognitionActiveStateChanged(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("+++++++++++ SttModelOnRecognitionActiveStateChanged");
            OnPropertyChanged(nameof(RecognitionActive));
        }

        /// <summary>
        /// Handles recognition error.
        /// Executes command which shows proper message.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void SttModelOnRecognitionError(object sender, EventArgs eventArgs)
        {
            RecognitionFailedInfoCommand?.Execute(null);
        }

        /// <summary>
        /// Handles STT service error.
        /// Executes command which shows proper message.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="serviceErrorEventArgs">Event arguments.</param>
        private void SttModelOnServiceError(object sender, EventArgs args)
        {
            ShowErrorDialog("Service Error", "Service Error Message");
        }

        void ShowErrorDialog(string err, string msg)
        {
            ServiceError = err;
            ServiceErrorMessage = msg;

            Device.StartTimer(TimeSpan.Zero, () =>
            {
                ServiceErrorInfoCommand?.Execute(null);
                // return false to run the timer callback only once
                return false;
            });
        }

        #endregion
    }
}
