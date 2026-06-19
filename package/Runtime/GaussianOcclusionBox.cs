using UnityEngine;

namespace GaussianSplatting.Runtime
{
    /// <summary>
    /// Attach to a GameObject that has a <see cref="GaussianSplatRenderer"/>. Maintains
    /// a child Transform sized to the splat asset's bounding box — an oriented box aligned
    /// to this object (the asset's local AABB carried through this transform's rotation and
    /// scale). Assign that child to a consumer that wants to know the splats' volume, e.g.
    /// a point-cloud renderer's "occlusion box" field, so the consumer can carve out exactly
    /// where the splats are.
    ///
    /// The child's local-to-world maps a unit cube [-0.5, 0.5]^3 onto the asset bounds, so a
    /// consumer can test a world point by mapping it through the child's worldToLocalMatrix
    /// and checking it lies within [-0.5, 0.5] on all axes.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(GaussianSplatRenderer))]
    public class GaussianOcclusionBox : MonoBehaviour
    {
        [Tooltip("Auto-created child Transform sized to the splat bounds. Assign this to the consumer's occlusion-box field.")]
        public Transform occlusionBox;

        [Tooltip("Extra margin (object-local meters) added to every side of the box. " +
                 "Splats extend past their center bounds, so a small positive value avoids a rim.")]
        public float padding = 0f;

        private GaussianSplatRenderer _renderer;

        private void OnEnable() { _renderer = GetComponent<GaussianSplatRenderer>(); }

        private void Update()
        {
            if (_renderer == null) _renderer = GetComponent<GaussianSplatRenderer>();
            if (_renderer == null || _renderer.asset == null) return;

            if (occlusionBox == null)
            {
                var go = new GameObject("OcclusionBox (auto)");
                occlusionBox = go.transform;
                occlusionBox.SetParent(transform, false);
            }

            Vector3 min = _renderer.asset.boundsMin;
            Vector3 max = _renderer.asset.boundsMax;
            occlusionBox.localPosition = (min + max) * 0.5f;
            occlusionBox.localRotation = Quaternion.identity;
            occlusionBox.localScale = (max - min) + Vector3.one * (2f * padding);
        }
    }
}
