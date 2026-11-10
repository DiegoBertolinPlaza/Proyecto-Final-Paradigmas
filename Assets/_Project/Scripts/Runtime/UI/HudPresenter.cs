using System;
using MatriculaDeHonor.Core;

namespace MatriculaDeHonor.UI
{
    /// <summary>Lo que el HUD sabe pintar. Hoy lo implementa la UI provisional (IMGUI);
    /// mañana una UI con UGUI o UI Toolkit, sin tocar el presentador.</summary>
    public interface IHudView
    {
        void SetLives(int lives);
        void SetCredits(int credits);
        void SetHealth(int current, int max, int shield);
        void SetHonors(int collected, int total);
        void SetPowerUp(string displayName, bool active, float duration);
    }

    /// <summary>
    /// Presentador del HUD (MVP): escucha el bus de eventos y traduce a llamadas a la vista.
    /// Ni el jugador ni el GameManager saben que existe un HUD.
    /// </summary>
    public sealed class HudPresenter : IDisposable
    {
        private readonly IHudView view;

        public HudPresenter(IHudView view)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));

            EventBus<LivesChangedEvent>.Subscribe(OnLives);
            EventBus<CreditsChangedEvent>.Subscribe(OnCredits);
            EventBus<PlayerHealthChangedEvent>.Subscribe(OnHealth);
            EventBus<HonorsChangedEvent>.Subscribe(OnHonors);
            EventBus<PowerUpChangedEvent>.Subscribe(OnPowerUp);
        }

        public void Dispose()
        {
            EventBus<LivesChangedEvent>.Unsubscribe(OnLives);
            EventBus<CreditsChangedEvent>.Unsubscribe(OnCredits);
            EventBus<PlayerHealthChangedEvent>.Unsubscribe(OnHealth);
            EventBus<HonorsChangedEvent>.Unsubscribe(OnHonors);
            EventBus<PowerUpChangedEvent>.Unsubscribe(OnPowerUp);
        }

        private void OnLives(LivesChangedEvent evt) => view.SetLives(evt.Lives);
        private void OnCredits(CreditsChangedEvent evt) => view.SetCredits(evt.Total);
        private void OnHealth(PlayerHealthChangedEvent evt) => view.SetHealth(evt.Current, evt.Max, evt.Shield);
        private void OnHonors(HonorsChangedEvent evt) => view.SetHonors(evt.Collected, evt.TotalInLevel);
        private void OnPowerUp(PowerUpChangedEvent evt) => view.SetPowerUp(evt.DisplayName, evt.Active, evt.Duration);
    }
}
