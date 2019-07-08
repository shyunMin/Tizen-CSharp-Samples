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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace SpeechToText.Model
{
    /// <summary>
    /// The model class handling speech-to-text logic.
    /// </summary>
    class TextToSpeechModel
    {
        #region fields

        /// <summary>
        /// Key used to store STT language in application state.
        /// </summary>
        private static readonly string STATE_SETTINGS_LANGUAGE_KEY = "language";

        /// <summary>
        /// Key used to store sounds status (on/off) in application state.
        /// </summary>
        private static readonly string STATE_SETTINGS_SOUND_ON_KEY = "sound_on";

        /// <summary>
        /// Private backing field for Language property.
        /// </summary>
        private string _language;

        /// <summary>
        /// Dictionary holding model state (persistent).
        /// </summary>
        private IDictionary<string, object> _state;

        /// <summary>
        /// An instance of the STT service.
        /// </summary>
        private readonly SpeechToTextApiManager _sttService;

        /// <summary>
        /// Private backing field for SoundOn property.
        /// </summary>
        private bool _soundOn;

        /// <summary>
        /// Flag indicating if recognition was stopped.
        /// Stopped recognition cannot be continued (only restarted).
        /// </summary>
        private bool _recognitionStopped;

        #endregion

        #region properties

        /// <summary>
        /// Event invoked when the recognition result was changed.
        /// </summary>
        public event EventHandler<EventArgs> ResultChanged;

        /// <summary>
        /// Event invoked when recognition state was changed (on/off).
        /// </summary>
        public event EventHandler<EventArgs> RecognitionActiveStateChanged;

        /// <summary>
        /// Event invoked when STT service error occurs.
        /// Event arguments contains detailed information about the error.
        /// </summary>
        public event EventHandler<IServiceErrorEventArgs> ServiceError;

        /// <summary>
        /// Event invoked when recognition error occurs.
        /// </summary>
        public event EventHandler<EventArgs> RecognitionError;

        /// <summary>
        /// A collection of languages supported by the speech-to-text model.
        ///
        /// The language is specified as an ISO 3166 alpha-2 two letter country-code
        /// followed by ISO 639-1 for the two-letter language code.
        /// </summary>
        public IEnumerable<string> SupportedLanguages => _sttService.SupportedLanguages;

        /// <summary>
        /// Flag indicating if model is ready for processing speech and changing settings.
        /// </summary>
        public bool Ready => _sttService.Ready;

        /// <summary>
        /// Current STT model language (code).
        /// </summary>
        public string Language
        {
            get => _language;
            set
            {
                _state[STATE_SETTINGS_LANGUAGE_KEY] = value;
                _language = value;
                Application.Current.SavePropertiesAsync();
            }
        }

        /// <summary>
        /// Flag indicating if model sounds are on.
        /// </summary>
        public bool SoundOn
        {
            get => _soundOn;
            set
            {
                _soundOn = value;


                _state[STATE_SETTINGS_SOUND_ON_KEY] = _soundOn;
                Application.Current.SavePropertiesAsync();
            }
        }

        /// <summary>
        /// Flag indicating if recognition is active.
        /// </summary>
        public bool RecognitionActive => _sttService.RecognitionActive;

        #endregion

        #region methods

        /// <summary>
        /// The model constructor.
        /// </summary>
        /// <param name="state">Persistent storage used to save state.</param>
        public TextToSpeechModel(IDictionary<string, object> state)
        {
            _state = state;
            _sttService = new SpeechToTextApiManager();
            //_sttService.RecognitionResult += SttServiceOnRecognitionResult;
            _sttService.RecognitionActiveStateChanged += SttServiceOnRecognitionActiveStateChanged;
            _sttService.RecognitionError += SttServiceOnRecognitionError;
            _sttService.ServiceError += SttServiceOnServiceError;
        }

        /// <summary>
        /// Returns true if all required privileges are granted, false otherwise.
        /// </summary>
        /// <returns>Task with check result.</returns>
        public async Task<bool> CheckPrivileges()
        {
            return await _sttService.CheckPrivileges();
        }

        /// <summary>
        /// Initializes the model.
        /// </summary>
        /// <returns>Initialization task.</returns>
        public async Task Init()
        {
            await _sttService.Init();

            RestoreState();
        }

        /// <summary>
        /// Handles STT service error event.
        /// Invokes own (class) similar event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="serviceErrorEventArgs">Event arguments.</param>
        private void SttServiceOnServiceError(object sender, IServiceErrorEventArgs serviceErrorEventArgs)
        {
            ServiceError?.Invoke(this, serviceErrorEventArgs);
        }

        /// <summary>
        /// Handles recognition error event.
        /// Invokes own (class) similar event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void SttServiceOnRecognitionError(object sender, EventArgs eventArgs)
        {
            RecognitionError?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Restores state of the model (settings).
        /// </summary>
        private void RestoreState()
        {
            Language = _state.ContainsKey(STATE_SETTINGS_LANGUAGE_KEY) ?
                (string)_state[STATE_SETTINGS_LANGUAGE_KEY] : _sttService.DefaultLanguage;

            SoundOn = _state.ContainsKey(STATE_SETTINGS_SOUND_ON_KEY) &&
                (bool)_state[STATE_SETTINGS_SOUND_ON_KEY];
        }

        /// <summary>
        /// Handles recognition active state change event.
        /// Fires own (class) similar event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private void SttServiceOnRecognitionActiveStateChanged(object sender, EventArgs eventArgs)
        {
            RecognitionActiveStateChanged?.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Starts the recognition.
        /// </summary>
        public void Start()
        {
            if (_recognitionStopped)
            {
                Clear();
                _recognitionStopped = false;
            }

            _sttService.Start(Language, RecognitionType.Free);
        }

        /// <summary>
        /// Pauses the recognition.
        /// </summary>
        public void Pause()
        {
            _sttService.Stop();
        }

        /// <summary>
        /// Stops the recognition.
        /// Locks recognition start until current result is cleared.
        /// </summary>
        public void Stop()
        {
            _sttService.Stop();
            _recognitionStopped = true;
        }

        /// <summary>
        /// Clears recognition result.
        /// Unlocks recognition start.
        /// </summary>
        public void Clear()
        {
            //_results.Clear();
            ResultChanged?.Invoke(this, new EventArgs());
        }

        #endregion
    }
}
