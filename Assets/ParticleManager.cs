using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

/// <summary>
/// Class for managing orbiting particles.
/// </summary>
public class ParticleManager : MonoBehaviour {
    /// <summary>
    /// How quickly to rotate, in degrees per second.
    /// </summary>
    public float timeScaler;

    /// <summary>
    /// The radius of the orbit.
    /// </summary>
    public float scale;

    [SerializeField]
    private Transform parent;

    [SerializeField]
    private GameObject particle;

    private List<GameObject> particles = new List<GameObject>();

    private void FixedUpdate()
    {
        float t = Time.fixedDeltaTime;
        t *= timeScaler;
        parent.localEulerAngles += new Vector3(0, t, 0);
    }

    /// <summary>
    /// Sets the number of particles in the orbit.
    /// </summary>
    /// <param name="Amount">The number of particles to show.</param>
    public void SetParticles(int Amount)
    {
        if (Amount < 0)
            throw new ArgumentException("Can't go below 0 particles.");
        int c = particles.Count;
        if (c == Amount)
            return;
        if (c < Amount)
        {
            for (int i = 0; i < Amount - c; i++)
            {
                var newParticle = Instantiate(particle, parent);
                particles.Add(newParticle);
            }
        }
        else
        {
            foreach (var i in particles.Take(c - Amount))
                Destroy(i);
            particles.RemoveRange(0, c - Amount);
        }
        ArrangeParticles();
    }

    /// <summary>
    /// Adds one particle to the orbit.
    /// </summary>
    public void AddParticle()
    {
        var newParticle = Instantiate(particle, parent);
        particles.Add(newParticle);
        ArrangeParticles();
    }

    /// <summary>
    /// Removes one particle from the orbit.
    /// </summary>
    public void RemoveParticle()
    {
        if (particles.Count <= 0)
            throw new OverflowException("Can't go below 0 particles.");
        particles.RemoveAt(0);
        ArrangeParticles();
    }

    private const float DEGTORAD = 2 * Mathf.PI / 360;

    private void ArrangeParticles()
    {
        if (particles.Count <= 0)
            return;
        float offset = 360 * DEGTORAD / particles.Count;
        for (int i = 0; i < particles.Count; i++)
        {
            // Rotates a particle around the scaled circle.
            particles[i].transform.localPosition = new Vector3(Mathf.Cos(i * offset) * scale, 0f, Mathf.Sin(i * offset) * scale);
        }
    }
}
