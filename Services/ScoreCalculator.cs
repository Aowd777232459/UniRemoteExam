using Microsoft.EntityFrameworkCore;
using UniRemoteExam.Data;

namespace UniRemoteExam.Services;

public sealed class ScoreCalculator
{
    private readonly UniRemoteExamDbContext _db;
    public ScoreCalculator(UniRemoteExamDbContext db) => _db = db;

    public async Task<ScoreSummary> CalculateAsync(int attemptId, int examId)
    {
        var questions = await _db.Questions.Where(q => q.ExamId == examId).ToListAsync();
        var answers = await _db.AttemptAnswers.Where(a => a.AttemptId == attemptId).ToListAsync();
        var keys = await _db.AnswerKeyItems.Where(k => k.ExamId == examId).ToListAsync();
        var manual = await _db.ManualScores.Where(m => m.AttemptId == attemptId).ToListAsync();

        decimal auto = 0m;
        foreach (var q in questions.Where(q => q.QuestionType is "MCQ" or "TF"))
        {
            var answer = answers.FirstOrDefault(a => a.QuestionId == q.QuestionId);
            var key = keys.FirstOrDefault(k => k.QuestionId == q.QuestionId);
            if (answer == null || key == null) continue;

            if (q.QuestionType == "MCQ" && key.CorrectChoiceId.HasValue && answer.SelectedChoiceId == key.CorrectChoiceId)
                auto += q.Points;
            else if (q.QuestionType == "TF" && key.CorrectBool.HasValue && answer.BoolAnswer == key.CorrectBool)
                auto += q.Points;
        }

        var manualScore = manual.Sum(m => m.Score);
        var maximum = questions.Sum(q => q.Points);
        var final = Math.Clamp(auto + manualScore, 0m, maximum);
        var percentage = maximum <= 0 ? 0m : Math.Round(final * 100m / maximum, 2);
        return new ScoreSummary(auto, manualScore, final, maximum, percentage);
    }
}

public readonly record struct ScoreSummary(decimal AutoScore, decimal ManualScore, decimal FinalScore, decimal MaximumScore, decimal Percentage);
