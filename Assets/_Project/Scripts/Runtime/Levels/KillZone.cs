using MatriculaDeHonor.Player;
using MatriculaDeHonor.World;

namespace MatriculaDeHonor.Levels
{
    /// <summary>El vacío bajo el nivel: tocarlo cuesta una convocatoria.</summary>
    public sealed class KillZone : PlayerTriggerVolume
    {
        protected override void OnPlayerEnter(PlayerController player)
        {
            if (player.IsAlive) player.Kill();
        }
    }
}
