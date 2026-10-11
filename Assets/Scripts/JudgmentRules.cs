using System;
using System.Collections.Generic;
using System.Globalization;

namespace Valhalla.Campaign
{
    public sealed class JudgmentEvidence
    {
        public int parries;
        public int dodges;
        public int eliteKills;
        public int bossKills;
        public int severeLimbs;
        public float closeSeconds;
        public float retreatSeconds;
        public float criticalSeconds;
        public float honor;
        public bool survived;
    }

    public sealed class JudgmentResult
    {
        public string title;
        public string reason;
        public int courage;
        public int endurance;
        public int dishonor;
        public bool valorCandidate;
        public bool survivorCandidate;
    }

    // Pure, deterministic Valhalla judgment: produces a title/reason and candidate flags only.
    // Candidate flags are read by other systems to unlock alternative tattoos (PlayerProfile.valorMarks /
    // survivorMarks). This class never mutates a profile or grants a stat buff.
    public static class JudgmentRules
    {
        private const float LongRetreatSeconds = 20f;

        public static JudgmentResult Evaluate(JudgmentEvidence e)
        {
            if (e == null) e = new JudgmentEvidence();

            int parries = Math.Max(0, e.parries);
            int dodges = Math.Max(0, e.dodges);
            int eliteKills = Math.Max(0, e.eliteKills);
            int bossKills = Math.Max(0, e.bossKills);
            int severeLimbs = Math.Max(0, e.severeLimbs);
            float closeSeconds = Math.Max(0f, e.closeSeconds);
            float retreatSeconds = Math.Max(0f, e.retreatSeconds);
            float criticalSeconds = Math.Max(0f, e.criticalSeconds);
            float honor = Math.Max(0f, e.honor);
            bool survived = e.survived;

            int courage = Math.Min(3, parries)
                + Math.Min(3, dodges)
                + Math.Min(3, eliteKills) * 2
                + Math.Min(2, bossKills) * 3
                + Math.Min(3, (int)(closeSeconds / 10f))
                + Math.Min(3, (int)(criticalSeconds / 5f));

            int dishonor = Math.Min(10, (int)(retreatSeconds / 5f));

            int endurance = Math.Min(7, severeLimbs + Math.Min(3, (int)(criticalSeconds / 5f)));

            bool valorCandidate = (courage - dishonor) >= 8 && honor >= 60f && retreatSeconds < LongRetreatSeconds;
            bool survivorCandidate = severeLimbs >= 1 && criticalSeconds >= 5f;
            bool longRetreat = retreatSeconds >= LongRetreatSeconds;

            string title;
            if (valorCandidate) title = "용맹의 발할라행";
            else if (survivorCandidate) title = "상처 입은 생존자";
            else if (longRetreat) title = "긴 도주의 그늘";
            else title = "증명되지 않은 전사";

            string reason = BuildReason(parries, dodges, eliteKills, bossKills, severeLimbs,
                closeSeconds, retreatSeconds, criticalSeconds, honor, survived);

            return new JudgmentResult
            {
                title = title,
                reason = reason,
                courage = courage,
                endurance = endurance,
                dishonor = dishonor,
                valorCandidate = valorCandidate,
                survivorCandidate = survivorCandidate
            };
        }

        private static string BuildReason(int parries, int dodges, int eliteKills, int bossKills, int severeLimbs,
            float closeSeconds, float retreatSeconds, float criticalSeconds, float honor, bool survived)
        {
            List<string> facts = new List<string> { survived ? "전장에서 살아남았다" : "전장에서 전사했다" };

            if (parries > 0) facts.Add("패링 " + parries + "회");
            if (dodges > 0) facts.Add("회피 " + dodges + "회");
            if (eliteKills > 0) facts.Add("정예 처치 " + eliteKills + "회");
            if (bossKills > 0) facts.Add("보스 처치 " + bossKills + "회");
            if (closeSeconds > 0f) facts.Add("근접 교전 " + Format(closeSeconds) + "초");
            if (criticalSeconds > 0f) facts.Add("위급 생존 " + Format(criticalSeconds) + "초");
            if (retreatSeconds > 0f) facts.Add("도주 " + Format(retreatSeconds) + "초");
            if (severeLimbs > 0) facts.Add("심각한 부상 " + severeLimbs + "곳");
            facts.Add("명예 " + Format(honor));

            return string.Join(", ", facts) + "를 근거로 평가했다.";
        }

        private static string Format(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }

    public static class JudgmentValidation
    {
        public static List<string> Run()
        {
            List<string> results = new List<string>();

            RunBraveDeathIsValorCandidate(results);
            RunQuietShortRetreatAllowsValor(results);
            RunLongFlightSuppressesValor(results);
            RunSurvivorNeedsSevereAndCritical(results);
            RunNegativeInputsAreSanitized(results);
            RunEvaluateIsDeterministic(results);

            return results;
        }

        private static void Check(List<string> results, string name, bool condition)
        {
            results.Add((condition ? "PASS: " : "FAIL: ") + name);
        }

        private static void RunBraveDeathIsValorCandidate(List<string> results)
        {
            JudgmentEvidence evidence = new JudgmentEvidence
            {
                parries = 5,
                dodges = 4,
                eliteKills = 3,
                bossKills = 1,
                closeSeconds = 40f,
                criticalSeconds = 20f,
                retreatSeconds = 0f,
                honor = 80f,
                survived = false
            };

            JudgmentResult result = JudgmentRules.Evaluate(evidence);

            Check(results, "A warrior who fought bravely but died still qualifies as a valor candidate and the reason reports death, not survival",
                result.valorCandidate && result.reason.Contains("전사했다") && !result.reason.Contains("살아남았다"));
        }

        private static void RunQuietShortRetreatAllowsValor(List<string> results)
        {
            JudgmentEvidence evidence = new JudgmentEvidence
            {
                parries = 5,
                dodges = 4,
                eliteKills = 3,
                bossKills = 1,
                closeSeconds = 40f,
                criticalSeconds = 20f,
                retreatSeconds = 10f,
                honor = 70f,
                survived = true
            };

            JudgmentResult result = JudgmentRules.Evaluate(evidence);

            Check(results, "A short, quiet retreat under the long-flight threshold does not disqualify a valor candidate",
                result.valorCandidate && result.title == "용맹의 발할라행");
        }

        private static void RunLongFlightSuppressesValor(List<string> results)
        {
            JudgmentEvidence evidence = new JudgmentEvidence
            {
                parries = 5,
                dodges = 4,
                eliteKills = 3,
                bossKills = 1,
                closeSeconds = 40f,
                criticalSeconds = 20f,
                retreatSeconds = 25f,
                honor = 80f,
                survived = true
            };

            JudgmentResult result = JudgmentRules.Evaluate(evidence);

            Check(results, "A long flight from battle suppresses valor candidacy even with otherwise strong courage and honor",
                !result.valorCandidate && result.title != "용맹의 발할라행");
        }

        private static void RunSurvivorNeedsSevereAndCritical(List<string> results)
        {
            JudgmentEvidence severeOnly = new JudgmentEvidence { severeLimbs = 1, criticalSeconds = 2f };
            JudgmentEvidence criticalOnly = new JudgmentEvidence { severeLimbs = 0, criticalSeconds = 8f };
            JudgmentEvidence both = new JudgmentEvidence { severeLimbs = 1, criticalSeconds = 8f };

            bool severeOnlyFails = !JudgmentRules.Evaluate(severeOnly).survivorCandidate;
            bool criticalOnlyFails = !JudgmentRules.Evaluate(criticalOnly).survivorCandidate;
            bool bothSucceeds = JudgmentRules.Evaluate(both).survivorCandidate;

            Check(results, "Survivor candidacy requires both a severe limb injury and at least five seconds in a critical state",
                severeOnlyFails && criticalOnlyFails && bothSucceeds);
        }

        private static void RunNegativeInputsAreSanitized(List<string> results)
        {
            JudgmentEvidence evidence = new JudgmentEvidence
            {
                parries = -5,
                dodges = -5,
                eliteKills = -5,
                bossKills = -5,
                severeLimbs = -5,
                closeSeconds = -40f,
                retreatSeconds = -40f,
                criticalSeconds = -40f,
                honor = -40f,
                survived = false
            };

            JudgmentResult result = JudgmentRules.Evaluate(evidence);

            Check(results, "Negative evidence inputs are clamped to zero and never produce negative or candidate outputs",
                result.courage == 0 && result.dishonor == 0 && result.endurance == 0
                && !result.valorCandidate && !result.survivorCandidate
                && result.title == "증명되지 않은 전사");
        }

        private static void RunEvaluateIsDeterministic(List<string> results)
        {
            JudgmentEvidence evidence = new JudgmentEvidence
            {
                parries = 2,
                dodges = 3,
                eliteKills = 1,
                bossKills = 0,
                severeLimbs = 1,
                closeSeconds = 15f,
                retreatSeconds = 8f,
                criticalSeconds = 6f,
                honor = 50f,
                survived = true
            };

            JudgmentResult first = JudgmentRules.Evaluate(evidence);
            JudgmentResult second = JudgmentRules.Evaluate(evidence);

            Check(results, "Evaluating the same evidence twice yields identical results",
                first.title == second.title && first.reason == second.reason
                && first.courage == second.courage && first.dishonor == second.dishonor
                && first.endurance == second.endurance
                && first.valorCandidate == second.valorCandidate
                && first.survivorCandidate == second.survivorCandidate);
        }
    }
}
