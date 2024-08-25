using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AiWorldGeneration
{
    public enum EditionMode
    {
        BASIC, EXPERT, PAINTING
    }

    public class UIManager : MonoBehaviour
    {
        [SerializeField, Tooltip("Compact UI")]
        bool compactMode;

        [Tooltip("Edition panel with additional options.")]
        [SerializeField]
        GameObject editionPanel;

        [Tooltip("Input field that contains the text prompt.")]
        [SerializeField]
        TMP_InputField inputField;

        [Tooltip("Slider that acts as a progress bar.")]
        [SerializeField]
        Slider slider;

        [Tooltip("Button to submit a new prompt.")]
        [SerializeField]
        Button submitButton;

        [Tooltip("Skybox creation button in the advanced mode.")]
        [SerializeField]
        Button[] skyboxButtons;


        [Header("Compact UI")]
        [Tooltip("Main dropdown menu to choose the edition mode.")]
        [SerializeField]
        TMP_Dropdown editionModeDropdown;

        [Tooltip("Button to toggle the audio recording.")]
        [SerializeField]
        Button recordingButton;

        [Header("Extended UI")]
        [Tooltip("Input field that contains the text prompt for the inpainting task.")]
        [SerializeField]
        TMP_InputField inpaintingInputField;

        [SerializeField, Tooltip("Toggle that sets the expert mode.")]
        Toggle expertModeToggle;


        /// <summary>
        /// Sets the text of the recording button to the provided message.
        /// </summary>
        /// <param name="message">The text to be displayed on the recording button.</param>
        /// <remarks>
        /// This method is used to update the text on the recording button to indicate the current state of the audio recording process.
        /// </remarks>
        public void SetRecordingButtonText(string message)
        {
            recordingButton.GetComponentInChildren<TMP_Text>().text = message;
        }


        /// <summary>
        /// Sets the text of the input field based on the provided input and the current UI mode.
        /// </summary>
        /// <param name="input">The text to be set in the input field.</param>
        /// <param name="inpaintingMode">
        /// A boolean indicating whether the UI is in inpainting mode or not.
        /// If true, the text is set in the inpainting input field.
        /// If false, the text is set in the regular input field.
        /// </param>
        /// <remarks>
        /// This function also resets the recording button text to "Start Recording" and enables the recording button.
        /// </remarks>
        public void SetInputFieldText(string input, bool inpaintingMode)
        {
            if (inpaintingMode)
            {
                inpaintingInputField.text = input;
            }
            else
            {
                inputField.text = input;
            }

            SetRecordingButtonText("Start Recording");
            recordingButton.interactable = true;
        }

        /// <summary>
        /// Sets the UI to indicate that audio analysis is in progress.
        /// </summary>
        /// <remarks>
        /// This method changes the text on the recording button to "Analysing..." and disables the button.
        /// It is called when the audio analysis process starts.
        /// </remarks>
        public void SetAudioAnalyse()
        {
            SetRecordingButtonText("Analysing...");
            recordingButton.interactable = false;
        }

        /// <summary>
        /// Updates the UI elements related to the generation process.
        /// </summary>
        /// <param name="generationStarting">
        /// A boolean indicating whether the generation process is starting or not.
        /// If true, the progress bar will be activated and the submit button will be disabled.
        /// If false, the progress bar will be deactivated and the submit button will be enabled.
        /// </param>
        public void SetGenerationChange(bool generationStarting)
        {
            slider.gameObject.SetActive(generationStarting);
            submitButton.enabled = !generationStarting;
        }

        /// <summary>
        /// Retrieves the current text prompt from the input field.
        /// </summary>
        /// <returns>The text currently in the input field.</returns>
        public string GetPrompt()
        {
            return inputField.text;
        }

        /// <summary>
        /// Retrieves the prompt for the inpainting task based on the current UI mode.
        /// </summary>
        /// <returns>The text prompt for the inpainting task.</returns>
        public string GetInpaintingPrompt()
        {
            return compactMode ? inputField.text : inpaintingInputField.text;
        }

        /// <summary>
        /// Retrieves the edition mode based on the selected dropdown value.
        /// </summary>
        /// <param name="value">
        /// The index of the selected option in the edition mode dropdown.
        /// </param>
        /// <returns>
        /// The corresponding edition mode based on the selected dropdown value.
        /// If the selected option is "skybox creation", returns EditionMode.BASIC.
        /// If the selected option is "skybox edition", returns EditionMode.PAINTING.
        /// If the selected option is "expert mode", returns EditionMode.EXPERT.
        /// Throws an InvalidOperationException if the selected dropdown value does not match any of the expected options.
        /// </returns>
        public EditionMode GetEditionMode(int value)
        {
            string editionModeText = editionModeDropdown.options[value].text.ToLower();
            return editionModeText switch
            {
                "skybox creation" => EditionMode.BASIC,
                "skybox edition" => EditionMode.PAINTING,
                "expert mode" => EditionMode.EXPERT,
                _ => throw new System.InvalidOperationException("Unknown dropdown value: " + editionModeText),
            };
        }

        /// <summary>
        /// Retrieves the edition mode based on the current UI mode and input parameters.
        /// </summary>
        /// <param name="inpaintingMode">
        /// A boolean indicating whether the UI is in inpainting mode or not.
        /// </param>
        /// <returns>
        /// Returns the corresponding edition mode based on the current UI mode and input parameters.
        /// </returns>
        public EditionMode GetEditionMode(bool inpaintingMode)
        {
            if (compactMode)
            {
                return GetEditionMode(editionModeDropdown.value);
            }
            if (inpaintingMode)
                return EditionMode.PAINTING;
            if (expertModeToggle.isOn)
                return EditionMode.EXPERT;
            return EditionMode.BASIC;
        }

        /// <summary>
        /// Sets the visibility of the skybox buttons based on the current phase.
        /// </summary>
        /// <param name="skyboxPhase">
        /// The current phase of the skybox creation process.
        /// The function will activate the button corresponding to the current phase and deactivate all other buttons.
        /// </param>
        public void SetActiveButtons(int skyboxPhase)
        {
            for (var i = 0; i < skyboxButtons.Length; i++)
            {
                skyboxButtons[i].interactable = i < skyboxPhase;
            }
        }

        /// <summary>
        /// Update the progress bar.
        /// </summary>
        public void SetProgressBar(float value)
        {
            slider.value = value;
        }

        /// <summary>
        /// Retrieves the current expert mode status based on the UI mode.
        /// </summary>
        /// <returns>
        /// Returns true if expert mode is enabled, false otherwise.
        /// In compact mode, it checks the selected dropdown option to determine expert mode.
        /// In non-compact mode, it uses the toggle state of the expert mode toggle.
        /// </returns>
        public bool IsModeExpert()
        {
            if (compactMode)
            {
                return editionModeDropdown.options[editionModeDropdown.value].text.ToLower() == "expert mode";
            }
            return expertModeToggle.isOn;
        }

        /// <summary>
        /// Sets the edition mode of the UI based on the selected dropdown value.
        /// </summary>
        /// <param name="newValue">
        /// The new value to set for the edition mode dropdown.
        /// This value corresponds to the index of the selected option in the dropdown.
        /// </param>
        /// <remarks>
        /// This function updates the value of the edition mode dropdown and triggers the
        /// onValueChanged event of the dropdown to reflect the change in the UI.
        /// </remarks>
        public void SetEditionMode(int newValue)
        {
            editionModeDropdown.value = newValue;
            editionModeDropdown.onValueChanged.Invoke(newValue);
        }
    }
}
