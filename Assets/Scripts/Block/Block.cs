using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Block : MonoBehaviour
{
    public GridCell currentCell;
    public BlockMaterialData blockMaterialData;
    public bool isPopping = false;
    public LayerMask gridLayer;
    public List<Vector2Int> subCellIn;

    [Header("Pop FX")]
    public int popParticleCount = 24;
    public float popPunchScale = 1.25f;
    public float popPunchDuration = 0.12f;
    public float popShrinkDuration = 0.18f;
    public Material popParticleMaterial; // URP-compatible particle material (optional via Inspector)
    private static Material sDefaultURPParticleMat;

    public void findCurrentCell(GridCell cell)
    {
        if (cell == null) return;

        if (currentCell != null && currentCell != cell)
        {
            currentCell.isOccupied = false;
            currentCell.currentHolder = null;
        }

        // Always set holder and mark occupied
        currentCell = cell;
        cell.isOccupied = true;
        var holder = GetComponentInParent<BlockHolder>();
        cell.currentHolder = holder;
    }

    private void OnDestroy()
    {
        var holder = GetComponentInParent<BlockHolder>();
        if (holder != null)
        {
            holder.NotifyChildDestroyed(this);
        }
        else if (currentCell != null)
        {
            currentCell.isOccupied = false;
            currentCell.currentHolder = null;
        }
    }

    private void OnDisable()
    {
        var holder = GetComponentInParent<BlockHolder>();
        if (holder != null)
        {
            holder.NotifyChildDestroyed(this);
        }
        else if (currentCell != null)
        {
            currentCell.isOccupied = false;
            currentCell.currentHolder = null;
        }
    }

    public void PlayPopEffect()
    {
        if (this == null || isPopping) return;
        isPopping = true;

        // Only start coroutine if this component is active and enabled
        if (isActiveAndEnabled && gameObject.activeInHierarchy)
        {
            StartCoroutine(PopAnimation());
        }
        else
        {
            // Fallback: run immediate FX and destroy to avoid coroutine error
            var am = AudioManager.instance;
            if (am != null) am.playDestroyBlock();

            EmitPopParticles();
            Destroy(gameObject);
        }
    }

    private IEnumerator PopAnimation()
    {
        var am = AudioManager.instance;
        if (am != null) am.playDestroyBlock();

        try
        {
            EmitPopParticles();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("PopFX failed: " + ex.Message);
        }

        Vector3 originScale = transform.localScale;
        Quaternion originRot = transform.localRotation;
        float punch = popPunchDuration;
        float shrink = popShrinkDuration;
        float randRot = Random.Range(-12f, 12f);

        float t = 0f;
        while (t < punch)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / punch);
            float ease = 1f - Mathf.Pow(1f - p, 2f); // ease-out quad
            float s = Mathf.Lerp(1f, popPunchScale, ease);
            transform.localScale = originScale * s;
            transform.localRotation = originRot * Quaternion.Euler(0f, 0f, randRot * ease);
            yield return null;
        }

        t = 0f;
        while (t < shrink)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / shrink);
            float ease = p * p; // ease-in quad
            float s = Mathf.Lerp(popPunchScale, 0f, ease);
            transform.localScale = originScale * s;
            transform.localRotation = originRot * Quaternion.Euler(0f, 0f, randRot * (1f - ease));
            yield return null;
        }
        AudioManager.instance.playDestroyBlock();
        Destroy(gameObject);
    }

    private void EmitPopParticles()
    {
        Color tint = GetBlockTint();

        var psGO = new GameObject("PopFX");
        psGO.transform.position = transform.position;
        var ps = psGO.AddComponent<ParticleSystem>();

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.playOnAwake = false;

        var main = ps.main;
        main.loop = false;
        main.duration = 0.6f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
        main.gravityModifier = 0.2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startColor = new ParticleSystem.MinMaxGradient(tint);

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.05f;

        var emission = ps.emission;
        emission.enabled = false;
        emission.rateOverTime = 0f;

        var psr = ps.GetComponent<ParticleSystemRenderer>();
        psr.renderMode = ParticleSystemRenderMode.Billboard;
        var mat = EnsureURPParticleMaterial();
        if (mat != null) psr.material = mat;

        ps.Emit(popParticleCount);
        ps.Play();

        Destroy(psGO, 1.0f);
    }

    private Material EnsureURPParticleMaterial()
    {
        if (popParticleMaterial != null) return popParticleMaterial;
        if (sDefaultURPParticleMat != null) return sDefaultURPParticleMat;

        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) return null;

        sDefaultURPParticleMat = new Material(sh);
        sDefaultURPParticleMat.name = "PopFX_Runtime_URP_Unlit";
        sDefaultURPParticleMat.hideFlags = HideFlags.DontSave;

        if (sDefaultURPParticleMat.HasProperty("_BaseColor"))
            sDefaultURPParticleMat.SetColor("_BaseColor", Color.white);
        else if (sDefaultURPParticleMat.HasProperty("_Color"))
            sDefaultURPParticleMat.SetColor("_Color", Color.white);

        return sDefaultURPParticleMat;
    }

    private Color GetBlockTint()
    {
        var r = GetComponent<Renderer>();
        if (r != null && r.material != null)
        {
            var mat = r.material;

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                Debug.Log($"Tint from Renderer _BaseColor: {c}");
                return c;
            }
            if (mat.HasProperty("_Color"))
            {
                var c = mat.GetColor("_Color");
                Debug.Log($"Tint from Renderer _Color: {c}");
                return c;
            }

            var c2 = mat.color;
            Debug.Log($"Tint from Renderer material.color: {c2}");
            return c2;
        }

        if (blockMaterialData != null && blockMaterialData.material != null)
        {
            var mat = blockMaterialData.material;

            if (mat.HasProperty("_BaseColor"))
            {
                var c = mat.GetColor("_BaseColor");
                Debug.Log($"Tint from Scriptable _BaseColor: {c}");
                return c;
            }
            if (mat.HasProperty("_Color"))
            {
                var c = mat.GetColor("_Color");
                Debug.Log($"Tint from Scriptable _Color: {c}");
                return c;
            }

            var c3 = mat.color;
            Debug.Log($"Tint from Scriptable material.color: {c3}");
            return c3;
        }

        Debug.Log("Tint default: white");
        return Color.white;
    }

    public void SetMaterialBlock(BlockMaterialData data)
    {
        blockMaterialData = data;
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = data.material;
    }
}
