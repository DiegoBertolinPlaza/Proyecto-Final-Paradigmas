using MatriculaDeHonor.Core;
using MatriculaDeHonor.Player;
using MatriculaDeHonor.World;
using UnityEngine;

namespace MatriculaDeHonor.Levels
{
    public sealed class Checkpoint : PlayerTriggerVolume
    {
        [SerializeField] private Renderer indicator;
        [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 0.3f);

        private bool reached;

        protected override void OnPlayerEnter(PlayerController player)
        {
            if (reached || !player.IsMainPlayer) return;

            reached = true;
            EventBus<CheckpointReachedEvent>.Raise(new CheckpointReachedEvent(transform.position + Vector3.up * 0.5f, transform.rotation));

            if (indicator != null)
            {
                indicator.material.color = activeColor;
            }
        }
    }
}
