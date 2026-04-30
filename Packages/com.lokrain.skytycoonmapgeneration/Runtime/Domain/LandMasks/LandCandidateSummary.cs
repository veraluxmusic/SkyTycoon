#nullable enable

using System.Globalization;

namespace Lokrain.SkyTycoon.MapGeneration.Domain.LandMasks
{
    /// <summary>
    /// Deterministic diagnostic summary for a temporary land-candidate mask derived from
    /// a scalar height field by selecting the highest-valued cells.
    ///
    /// This is intentionally not a final land/water generation result. It is an observability
    /// contract used to evaluate whether a source height field is suitable input for later
    /// percentile-cut, connectivity and compensation stages.
    /// </summary>
    public struct LandCandidateSummary
    {
        public bool IsValid;

        public int Width;
        public int Height;
        public int SampleCount;

        public float TargetLandPercent;
        public int TargetLandCellCount;

        public float Threshold;
        public int SelectedLandCellCount;
        public float SelectedLandPercent;

        public int NonFiniteSourceCellCount;

        public int ComponentCount4Connected;
        public int LargestComponentCellCount;
        public float LargestComponentPercentOfSelected;

        public int SecondaryComponentCellCount;
        public int BorderLandCellCount;
        public int IsolatedLandCellCount;

        public readonly bool HasLand => SelectedLandCellCount > 0;
        public readonly bool IsSingleComponentCandidate => SelectedLandCellCount > 0 && ComponentCount4Connected == 1;
        public readonly bool HasSecondaryComponents => ComponentCount4Connected > 1;

        public static LandCandidateSummary CreateInvalid()
        {
            return new LandCandidateSummary
            {
                IsValid = false,
                Width = 0,
                Height = 0,
                SampleCount = 0,
                TargetLandPercent = 0f,
                TargetLandCellCount = 0,
                Threshold = 0f,
                SelectedLandCellCount = 0,
                SelectedLandPercent = 0f,
                NonFiniteSourceCellCount = 0,
                ComponentCount4Connected = 0,
                LargestComponentCellCount = 0,
                LargestComponentPercentOfSelected = 0f,
                SecondaryComponentCellCount = 0,
                BorderLandCellCount = 0,
                IsolatedLandCellCount = 0
            };
        }

        public override readonly string ToString()
        {
            if (!IsValid)
                return "Invalid land-candidate summary";

            CultureInfo culture = CultureInfo.InvariantCulture;

            return "targetLand="
                + TargetLandPercent.ToString("0.###", culture)
                + "% selected="
                + SelectedLandCellCount.ToString(culture)
                + "/"
                + SampleCount.ToString(culture)
                + " threshold="
                + Threshold.ToString("0.######", culture)
                + " components4="
                + ComponentCount4Connected.ToString(culture)
                + " largest="
                + LargestComponentCellCount.ToString(culture)
                + " largest%="
                + LargestComponentPercentOfSelected.ToString("0.###", culture)
                + " secondaryCells="
                + SecondaryComponentCellCount.ToString(culture)
                + " borderCells="
                + BorderLandCellCount.ToString(culture)
                + " isolatedCells="
                + IsolatedLandCellCount.ToString(culture)
                + " nonFiniteSource="
                + NonFiniteSourceCellCount.ToString(culture);
        }
    }
}