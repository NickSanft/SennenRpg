using System;

namespace SennenRpg.Core.Data;

/// <summary>
/// 5×5 transition probability matrix for weather rolls.
/// Indexed as [from][to] where 0..3 = Sunny/Foggy/Stormy/Snowy and 4 = Aurora.
/// Rows must sum to 1.0; <see cref="Validate"/> enforces this in tests.
///
/// Default table (see <see cref="Default"/>) has sticky-Snowy behavior and makes
/// Aurora reachable only from Sunny at 1%, always returning to Sunny the next roll.
/// </summary>
public sealed class WeatherTransitionTable
{
    /// <summary>Row-major 5×5 matrix. Access via <see cref="Prob"/>.</summary>
    public double[,] Matrix { get; }

    public WeatherTransitionTable(double[,] matrix)
    {
        if (matrix.GetLength(0) != 5 || matrix.GetLength(1) != 5)
            throw new ArgumentException("Weather transition matrix must be 5×5.", nameof(matrix));
        Matrix = matrix;
    }

    public double Prob(WeatherType from, WeatherType to) => Matrix[(int)from, (int)to];

    /// <summary>
    /// Default region-agnostic transition table.
    /// Sunny is only mildly sticky so weather changes feel lively; Snowy is moderately
    /// sticky so winter regions hold; Aurora is a 1% rare reachable only from Sunny and
    /// always resolves back to Sunny the next roll.
    /// </summary>
    public static readonly WeatherTransitionTable Default = new(new double[,]
    {
        //            Sunny  Foggy  Stormy Snowy  Aurora
        /* Sunny  */ { 0.40, 0.27, 0.22, 0.10, 0.01 },
        /* Foggy  */ { 0.45, 0.30, 0.15, 0.10, 0.00 },
        /* Stormy */ { 0.45, 0.20, 0.25, 0.10, 0.00 },
        /* Snowy  */ { 0.30, 0.15, 0.15, 0.40, 0.00 },
        /* Aurora */ { 1.00, 0.00, 0.00, 0.00, 0.00 }, // Aurora → Sunny always
    });

    /// <summary>
    /// Returns true when every row sums to approximately 1.0 (within tolerance).
    /// </summary>
    public bool Validate(double tolerance = 1e-9)
    {
        for (int from = 0; from < 5; from++)
        {
            double sum = 0;
            for (int to = 0; to < 5; to++)
                sum += Matrix[from, to];
            if (Math.Abs(sum - 1.0) > tolerance)
                return false;
        }
        return true;
    }
}
