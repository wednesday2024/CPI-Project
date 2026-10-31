using ClubPenguin.Audio;
using Disney.MobileNetwork;
using UnityEngine;
using UnityEngine.UI;

namespace ClubPenguin.UI
{
    public class SoundsPanelController : MonoBehaviour
    {
        public Slider MusicVolumeSlider;

        public Slider SoundsVolumeSlider;

        public Toggle ResetVolumeButton;

        public Toggle MuteButton;

        private GameSettings gameSettings;

        private float previousMusicVolume = 1f;
        private float previousSfxVolume = 1f;

        private bool isMuted;

        private void Start()
        {
            gameSettings = Service.Get<GameSettings>();

            MusicVolumeSlider.SetValueWithoutNotify(gameSettings.MusicVolume);
            SoundsVolumeSlider.SetValueWithoutNotify(gameSettings.SfxVolume);

            previousMusicVolume = MusicVolumeSlider.value;
            previousSfxVolume = SoundsVolumeSlider.value;

            isMuted = false;

            if (MuteButton != null)
            {
                MuteButton.SetIsOnWithoutNotify(false);
            }
        }

        private void OnEnable()
        {
            MusicVolumeSlider.onValueChanged.AddListener(onMusicVolumeChanged);
            SoundsVolumeSlider.onValueChanged.AddListener(onSfxVolumeChanged);

            if (ResetVolumeButton != null)
            {
                ResetVolumeButton.onValueChanged.AddListener(onResetVolumeButtonClicked);
            }

            if (MuteButton != null)
            {
                MuteButton.onValueChanged.AddListener(onMuteButtonClicked);
            }
        }

        private void OnDisable()
        {
            MusicVolumeSlider.onValueChanged.RemoveListener(onMusicVolumeChanged);
            SoundsVolumeSlider.onValueChanged.RemoveListener(onSfxVolumeChanged);

            if (ResetVolumeButton != null)
            {
                ResetVolumeButton.onValueChanged.RemoveListener(onResetVolumeButtonClicked);
            }

            if (MuteButton != null)
            {
                MuteButton.onValueChanged.RemoveListener(onMuteButtonClicked);
            }
        }

        private void onMusicVolumeChanged(float value)
        {
            if (!isMuted)
            {
                previousMusicVolume = value;
                Service.Get<AudioController>().SetMusicVolume(value);
                gameSettings.MusicVolume.SetValue(value);
            }
        }

        private void onSfxVolumeChanged(float value)
        {
            if (!isMuted)
            {
                previousSfxVolume = value;
                Service.Get<AudioController>().SetSFXVolume(value);
                gameSettings.SfxVolume.SetValue(value);
            }
        }

        private void onResetVolumeButtonClicked(bool value)
        {
            const float defaultVolume = 1f;

            previousMusicVolume = defaultVolume;
            previousSfxVolume = defaultVolume;

            if (isMuted)
            {
                isMuted = false;

                if (MuteButton != null)
                {
                    MuteButton.SetIsOnWithoutNotify(false);
                }
            }

            MusicVolumeSlider.value = defaultVolume;
            SoundsVolumeSlider.value = defaultVolume;

            Service.Get<AudioController>().SetMusicVolume(defaultVolume);
            Service.Get<AudioController>().SetSFXVolume(defaultVolume);

            gameSettings.MusicVolume.SetValue(defaultVolume);
            gameSettings.SfxVolume.SetValue(defaultVolume);
        }

        private void onMuteButtonClicked(bool value)
        {
            isMuted = value;

            if (isMuted)
            {
                previousMusicVolume = MusicVolumeSlider.value;
                previousSfxVolume = SoundsVolumeSlider.value;

                MusicVolumeSlider.SetValueWithoutNotify(0f);
                SoundsVolumeSlider.SetValueWithoutNotify(0f);

                Service.Get<AudioController>().SetMusicVolume(0f);
                Service.Get<AudioController>().SetSFXVolume(0f);
            }
            else
            {
                MusicVolumeSlider.SetValueWithoutNotify(previousMusicVolume);
                SoundsVolumeSlider.SetValueWithoutNotify(previousSfxVolume);

                Service.Get<AudioController>().SetMusicVolume(previousMusicVolume);
                Service.Get<AudioController>().SetSFXVolume(previousSfxVolume);
            }
        }
    }
}