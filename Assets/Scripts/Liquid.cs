using UnityEngine;

/// <summary>
/// This class handles how characters behave when in a liquid
/// </summary>
public class Liquid : MonoBehaviour
{
    // how tedious is it to walk in this liquid. 
    // be careful, too high of a viscosity and the player will stay stuck while in the liquid
    // and only gravity will be able to move them. 
    public float Viscosity;

    // viscous liquids do resist gravity a little, but do not ever negate it 100%
    public float SlowestGravityCanGo = 0.1f;

    // the math performed on gravity is unreversable, so I'm just storing it's value here to be able to restore it. 
    // this creates edgecases, but as long as you don't make touching liquids, it should be fine. 
    // I was more concerned about the ability to make many liquids than the interraction between them when I made this. 
    // I think this limit is not too hard and the ability to make multiple liquids easily and quickly is worth the tradeoff. 
    private Vector3 _gravityBackup;

    /// <summary>
    /// When the character enters the liquid, have them impacted by its viscosity. 
    /// </summary>
    /// <param name="other">thing colliding</param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent<MyCharacterController>(out MyCharacterController characterController))
        {
            characterController.IsSwimming = true;
            characterController.GroundAcceleration -= Viscosity;
            characterController.MaxGroundSpeed -= Viscosity;
            characterController.JumpStrength -= Viscosity;

            _gravityBackup = characterController.Gravity;
            float[] gravity = new float[3] {
                _gravityBackup.x,
                _gravityBackup.y,
                _gravityBackup.z
            };

            // 3 is hardcoded as a vector 3 will always have 3 axis
            for (int i = 0; i < 3; ++i)
            {
                // if 0, gravity does not affect this axis, so don't modify it
                if (gravity[i] < 0f)
                {
                    gravity[i] += Viscosity;

                    // liquid can make you fall slower, but not reverse gravity nor ever negate it entirely
                    if (gravity[i] >= 0f)
                    {
                        gravity[i] = -SlowestGravityCanGo;
                    }
                }
                else if (gravity[i] > 0f)
                {
                    gravity[i] -= Viscosity;

                    // liquid can make you fall slower, but not reverse gravity nor ever negate it entirely
                    if (gravity[i] <= 0f)
                    {
                        gravity[i] = SlowestGravityCanGo;
                    }
                }    
            }

            characterController.Gravity = new Vector3(gravity[0], gravity[1], gravity[2]);
        }
    }

    /// <summary>
    /// When the character exits the liquid, have them no longer be impacted by its viscosity. 
    /// </summary>
    /// <param name="other">thing no longer colliding</param>
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent<MyCharacterController>(out MyCharacterController characterController))
        {
            characterController.IsSwimming = false;
            characterController.GroundAcceleration += Viscosity;
            characterController.MaxGroundSpeed += Viscosity;
            characterController.JumpStrength += Viscosity;

            characterController.Gravity = _gravityBackup;
        }
    }

    // The solution to my two liquid problem would be to OnTriggerEnter and OnTriggerExit 
    // check all that is touching the character and only apply the highest viscosity, but 
    // I am not sure how to do that. The best solution I found was to try to cast a physic
    // sphere of the exact dimension of the character collider at its exact position... I 
    // feel like there is likely a better way and I am not even sure how to pull that off. 
}
