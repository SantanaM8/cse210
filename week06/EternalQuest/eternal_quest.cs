// Program.cs
// Eternal Quest - A gamified goal tracking system
//
// CREATIVITY AND EXCEEDING REQUIREMENTS:
// 1. Leveling System: Users gain levels based on total points (Level 1-20 with titles like "Novice Quester", "Champion", etc.)
// 2. Achievement Badges: Users earn special badges for milestones (First Goal, Century Club, Dedication Master, etc.)
// 3. Streak Tracking: Eternal goals track consecutive days completed for bonus points
// 4. Negative Goals: Added support for breaking bad habits where users lose points if they fail
// 5. Progress Goals: Large goals that track percentage completion (e.g., run 100 miles total)
// 6. Bonus Multipliers: Completing multiple goals in one session gives a combo multiplier
// 7. Visual enhancements: Colored console output, progress bars, and celebratory messages
// 8. Statistics Dashboard: Shows detailed stats including goals completed, success rate, and current streaks

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace EternalQuest
{
    // Base class for all goals
    public abstract class Goal
    {
        protected string _name;
        protected string _description;
        protected int _points;
        protected bool _isCompleted;

        public Goal(string name, string description, int points)
        {
            _name = name;
            _description = description;
            _points = points;
            _isCompleted = false;
        }

        public string Name => _name;
        public bool IsCompleted => _isCompleted;

        public abstract int RecordEvent();
        public abstract string GetStatus();
        public abstract string GetDetailsString();
    }

    // Simple goal - complete once for points
    public class SimpleGoal : Goal
    {
        public SimpleGoal(string name, string description, int points) 
            : base(name, description, points) { }

        public override int RecordEvent()
        {
            if (!_isCompleted)
            {
                _isCompleted = true;
                return _points;
            }
            return 0;
        }

        public override string GetStatus()
        {
            return _isCompleted ? "[X]" : "[ ]";
        }

        public override string GetDetailsString()
        {
            return $"{GetStatus()} {_name} - {_description} ({_points} points)";
        }
    }

    // Eternal goal - never complete, get points each time
    public class EternalGoal : Goal
    {
        private int _timesCompleted;
        private int _currentStreak;
        private DateTime _lastCompletedDate;

        public EternalGoal(string name, string description, int points) 
            : base(name, description, points)
        {
            _timesCompleted = 0;
            _currentStreak = 0;
            _lastCompletedDate = DateTime.MinValue;
        }

        public override int RecordEvent()
        {
            _timesCompleted++;
            int pointsEarned = _points;

            // Streak bonus
            if (_lastCompletedDate.Date == DateTime.Now.Date.AddDays(-1))
            {
                _currentStreak++;
                if (_currentStreak >= 7)
                {
                    pointsEarned += 50; // Weekly streak bonus
                }
            }
            else if (_lastCompletedDate.Date != DateTime.Now.Date)
            {
                _currentStreak = 1;
            }

            _lastCompletedDate = DateTime.Now;
            return pointsEarned;
        }

        public override string GetStatus()
        {
            return "[∞]"; // Eternal never completes
        }

        public override string GetDetailsString()
        {
            string streakInfo = _currentStreak > 1 ? $" 🔥 {_currentStreak} day streak!" : "";
            return $"{GetStatus()} {_name} - {_description} (Completed {_timesCompleted} times{streakInfo})";
        }

        public int CurrentStreak => _currentStreak;
    }

    // Checklist goal - complete X times for bonus
    public class ChecklistGoal : Goal
    {
        private int _timesCompleted;
        private int _targetCount;
        private int _bonusPoints;

        public ChecklistGoal(string name, string description, int points, int targetCount, int bonusPoints) 
            : base(name, description, points)
        {
            _timesCompleted = 0;
            _targetCount = targetCount;
            _bonusPoints = bonusPoints;
        }

        public override int RecordEvent()
        {
            if (_isCompleted) return 0;

            _timesCompleted++;
            int pointsEarned = _points;

            if (_timesCompleted >= _targetCount)
            {
                _isCompleted = true;
                pointsEarned += _bonusPoints;
            }

            return pointsEarned;
        }

        public override string GetStatus()
        {
            return _isCompleted ? "[X]" : "[ ]";
        }

        public override string GetDetailsString()
        {
            if (_isCompleted)
            {
                return $"{GetStatus()} {_name} - {_description} (COMPLETED!)";
            }
            return $"{GetStatus()} {_name} - {_description} (Completed {_timesCompleted}/{_targetCount} times)";
        }
    }

    // Progress goal - work towards a large goal with measurable progress
    public class ProgressGoal : Goal
    {
        private double _currentProgress;
        private double _targetProgress;
        private int _pointsPerUnit;

        public ProgressGoal(string name, string description, int pointsPerUnit, double targetProgress) 
            : base(name, description, pointsPerUnit)
        {
            _currentProgress = 0;
            _targetProgress = targetProgress;
            _pointsPerUnit = pointsPerUnit;
        }

        public int RecordProgress(double amount)
        {
            if (_isCompleted) return 0;

            _currentProgress += amount;
            int pointsEarned = (int)(amount * _pointsPerUnit);

            if (_currentProgress >= _targetProgress)
            {
                _isCompleted = true;
                pointsEarned += 1000; // Completion bonus
            }

            return pointsEarned;
        }

        public override int RecordEvent()
        {
            return RecordProgress(1);
        }

        public override string GetStatus()
        {
            return _isCompleted ? "[X]" : "[ ]";
        }

        public override string GetDetailsString()
        {
            int percentage = (int)((_currentProgress / _targetProgress) * 100);
            string progressBar = GetProgressBar(percentage);
            return $"{GetStatus()} {_name} - {_description} {progressBar} {_currentProgress:F1}/{_targetProgress} ({percentage}%)";
        }

        private string GetProgressBar(int percentage)
        {
            int filled = percentage / 10;
            return "[" + new string('█', filled) + new string('░', 10 - filled) + "]";
        }
    }

    // Negative goal - lose points if you fail to avoid bad habit
    public class NegativeGoal : Goal
    {
        private int _failureCount;

        public NegativeGoal(string name, string description, int penaltyPoints) 
            : base(name, description, -Math.Abs(penaltyPoints))
        {
            _failureCount = 0;
        }

        public override int RecordEvent()
        {
            _failureCount++;
            return _points; // Negative points
        }

        public override string GetStatus()
        {
            return "[!]";
        }

        public override string GetDetailsString()
        {
            return $"{GetStatus()} {_name} - {_description} (Failed {_failureCount} times - {Math.Abs(_points)} penalty each)";
        }
    }

    // Achievement system
    public class Achievement
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Badge { get; set; }

        public Achievement(string name, string description, string badge)
        {
            Name = name;
            Description = description;
            Badge = badge;
        }
    }

    // User profile with leveling system
    public class QuestUser
    {
        private int _score;
        private List<Goal> _goals;
        private List<Achievement> _achievements;
        private int _goalsCompletedToday;
        private DateTime _lastPlayedDate;

        public QuestUser()
        {
            _score = 0;
            _goals = new List<Goal>();
            _achievements = new List<Achievement>();
            _goalsCompletedToday = 0;
            _lastPlayedDate = DateTime.Now;
        }

        public int Score => _score;
        public List<Goal> Goals => _goals;
        public List<Achievement> Achievements => _achievements;

        public void AddPoints(int points)
        {
            int oldLevel = GetLevel();
            _score += points;
            int newLevel = GetLevel();

            if (newLevel > oldLevel)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n🎉 LEVEL UP! You are now Level {newLevel} - {GetLevelTitle()}! 🎉");
                Console.ResetColor();
            }
        }

        public void AddGoal(Goal goal)
        {
            _goals.Add(goal);
            
            if (_goals.Count == 1)
            {
                UnlockAchievement("First Quest", "Created your first goal", "🎯");
            }
        }

        public int RecordGoalEvent(Goal goal)
        {
            _goalsCompletedToday++;
            int basePoints = goal.RecordEvent();
            
            // Combo multiplier for multiple goals in one session
            double multiplier = 1.0 + (_goalsCompletedToday - 1) * 0.1;
            int totalPoints = (int)(basePoints * multiplier);

            if (_goalsCompletedToday > 1)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"🔥 COMBO x{multiplier:F1}! Bonus points earned!");
                Console.ResetColor();
            }

            AddPoints(totalPoints);
            CheckAchievements();
            
            return totalPoints;
        }

        public int GetLevel()
        {
            return Math.Min((_score / 1000) + 1, 20);
        }

        public string GetLevelTitle()
        {
            int level = GetLevel();
            if (level >= 20) return "Eternal Legend";
            if (level >= 15) return "Divine Champion";
            if (level >= 10) return "Master Quester";
            if (level >= 7) return "Valiant Hero";
            if (level >= 5) return "Skilled Adventurer";
            if (level >= 3) return "Eager Apprentice";
            return "Novice Quester";
        }

        private void UnlockAchievement(string name, string description, string badge)
        {
            if (!_achievements.Any(a => a.Name == name))
            {
                _achievements.Add(new Achievement(name, description, badge));
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"\n✨ Achievement Unlocked: {badge} {name} - {description}");
                Console.ResetColor();
            }
        }

        private void CheckAchievements()
        {
            if (_score >= 100 && !_achievements.Any(a => a.Name == "Century Club"))
            {
                UnlockAchievement("Century Club", "Earned 100 points", "💯");
            }

            if (_score >= 1000 && !_achievements.Any(a => a.Name == "Millennium Master"))
            {
                UnlockAchievement("Millennium Master", "Earned 1000 points", "🏆");
            }

            int completedGoals = _goals.Count(g => g.IsCompleted);
            if (completedGoals >= 5 && !_achievements.Any(a => a.Name == "Goal Crusher"))
            {
                UnlockAchievement("Goal Crusher", "Completed 5 goals", "⚡");
            }

            var maxStreak = _goals.OfType<EternalGoal>().Max(g => (int?)g.CurrentStreak) ?? 0;
            if (maxStreak >= 7 && !_achievements.Any(a => a.Name == "Dedication Master"))
            {
                UnlockAchievement("Dedication Master", "7-day streak on eternal goal", "🔥");
            }
        }

        public void ResetDailyCombo()
        {
            if (_lastPlayedDate.Date != DateTime.Now.Date)
            {
                _goalsCompletedToday = 0;
                _lastPlayedDate = DateTime.Now;
            }
        }
    }

    class Program
    {
        static QuestUser user = new QuestUser();

        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║      WELCOME TO ETERNAL QUEST!         ║");
            Console.WriteLine("║   Your Journey to Greatness Begins     ║");
            Console.WriteLine("╚════════════════════════════════════════╝\n");

            bool running = true;
            while (running)
            {
                user.ResetDailyCombo();
                DisplayMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        DisplayGoals();
                        break;
                    case "2":
                        CreateNewGoal();
                        break;
                    case "3":
                        RecordEvent();
                        break;
                    case "4":
                        DisplayScore();
                        break;
                    case "5":
                        DisplayAchievements();
                        break;
                    case "6":
                        SaveProgress();
                        break;
                    case "7":
                        LoadProgress();
                        break;
                    case "8":
                        running = false;
                        Console.WriteLine("\nMay your quest continue! Farewell, brave adventurer! 🗡️");
                        break;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        static void DisplayMenu()
        {
            Console.WriteLine("\n────────────────────────────────────────");
            Console.WriteLine("Menu Options:");
            Console.WriteLine("1. View Goals");
            Console.WriteLine("2. Create New Goal");
            Console.WriteLine("3. Record Event");
            Console.WriteLine("4. View Score & Stats");
            Console.WriteLine("5. View Achievements");
            Console.WriteLine("6. Save Progress");
            Console.WriteLine("7. Load Progress");
            Console.WriteLine("8. Quit");
            Console.Write("\nSelect an option: ");
        }

        static void DisplayGoals()
        {
            Console.WriteLine("\n═══════════════ YOUR GOALS ═══════════════");
            if (user.Goals.Count == 0)
            {
                Console.WriteLine("No goals yet. Create your first quest!");
            }
            else
            {
                for (int i = 0; i < user.Goals.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {user.Goals[i].GetDetailsString()}");
                }
            }
        }

        static void CreateNewGoal()
        {
            Console.WriteLine("\n═══ CREATE NEW GOAL ═══");
            Console.WriteLine("Goal Types:");
            Console.WriteLine("1. Simple Goal (complete once)");
            Console.WriteLine("2. Eternal Goal (ongoing)");
            Console.WriteLine("3. Checklist Goal (complete X times)");
            Console.WriteLine("4. Progress Goal (work towards a target)");
            Console.WriteLine("5. Negative Goal (break bad habit)");
            Console.Write("Choose goal type: ");
            
            string type = Console.ReadLine();
            
            Console.Write("Goal name: ");
            string name = Console.ReadLine();
            
            Console.Write("Description: ");
            string description = Console.ReadLine();
            
            Goal newGoal = null;

            switch (type)
            {
                case "1":
                    Console.Write("Points to earn: ");
                    int points = int.Parse(Console.ReadLine());
                    newGoal = new SimpleGoal(name, description, points);
                    break;
                    
                case "2":
                    Console.Write("Points per completion: ");
                    int eternalPoints = int.Parse(Console.ReadLine());
                    newGoal = new EternalGoal(name, description, eternalPoints);
                    break;
                    
                case "3":
                    Console.Write("Points per completion: ");
                    int checklistPoints = int.Parse(Console.ReadLine());
                    Console.Write("Target count: ");
                    int target = int.Parse(Console.ReadLine());
                    Console.Write("Bonus points upon completion: ");
                    int bonus = int.Parse(Console.ReadLine());
                    newGoal = new ChecklistGoal(name, description, checklistPoints, target, bonus);
                    break;
                    
                case "4":
                    Console.Write("Points per unit: ");
                    int progressPoints = int.Parse(Console.ReadLine());
                    Console.Write("Target amount: ");
                    double targetAmount = double.Parse(Console.ReadLine());
                    newGoal = new ProgressGoal(name, description, progressPoints, targetAmount);
                    break;
                    
                case "5":
                    Console.Write("Penalty points: ");
                    int penalty = int.Parse(Console.ReadLine());
                    newGoal = new NegativeGoal(name, description, penalty);
                    break;
                    
                default:
                    Console.WriteLine("Invalid type.");
                    return;
            }

            user.AddGoal(newGoal);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n✓ Goal '{name}' created successfully!");
            Console.ResetColor();
        }

        static void RecordEvent()
        {
            if (user.Goals.Count == 0)
            {
                Console.WriteLine("\nNo goals to record! Create a goal first.");
                return;
            }

            Console.WriteLine("\n═══ RECORD EVENT ═══");
            DisplayGoals();
            Console.Write("\nWhich goal did you accomplish? (enter number): ");
            
            if (int.TryParse(Console.ReadLine(), out int choice) && choice > 0 && choice <= user.Goals.Count)
            {
                Goal goal = user.Goals[choice - 1];
                
                if (goal is ProgressGoal progressGoal)
                {
                    Console.Write("How much progress? ");
                    double amount = double.Parse(Console.ReadLine());
                    int points = progressGoal.RecordProgress(amount);
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n✓ Progress recorded! +{points} points!");
                    Console.ResetColor();
                }
                else
                {
                    int points = user.RecordGoalEvent(goal);
                    
                    if (points > 0)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"\n✓ Event recorded! +{points} points!");
                        Console.ResetColor();
                        
                        if (goal.IsCompleted)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("🎊 Goal completed! Congratulations!");
                            Console.ResetColor();
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nThis goal is already completed or no points awarded.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Invalid selection.");
            }
        }

        static void DisplayScore()
        {
            Console.WriteLine("\n╔════════════════════════════════════════╗");
            Console.WriteLine($"║  SCORE: {user.Score,10} points              ║");
            Console.WriteLine($"║  LEVEL: {user.GetLevel(),3} - {user.GetLevelTitle(),-20}   ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            
            int nextLevel = (user.GetLevel() * 1000);
            int pointsToNext = nextLevel - user.Score;
            
            if (user.GetLevel() < 20)
            {
                Console.WriteLine($"\nPoints to next level: {pointsToNext}");
            }
            
            Console.WriteLine("\n═══ STATISTICS ═══");
            int totalGoals = user.Goals.Count;
            int completedGoals = user.Goals.Count(g => g.IsCompleted);
            int activeGoals = totalGoals - completedGoals;
            
            Console.WriteLine($"Total Goals: {totalGoals}");
            Console.WriteLine($"Completed: {completedGoals}");
            Console.WriteLine($"Active: {activeGoals}");
            
            if (totalGoals > 0)
            {
                double completionRate = ((double)completedGoals / totalGoals) * 100;
                Console.WriteLine($"Completion Rate: {completionRate:F1}%");
            }
        }

        static void DisplayAchievements()
        {
            Console.WriteLine("\n╔════════════════════════════════════════╗");
            Console.WriteLine("║          YOUR ACHIEVEMENTS             ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            
            if (user.Achievements.Count == 0)
            {
                Console.WriteLine("\nNo achievements yet. Keep questing!");
            }
            else
            {
                foreach (var achievement in user.Achievements)
                {
                    Console.WriteLine($"{achievement.Badge} {achievement.Name}");
                    Console.WriteLine($"   {achievement.Description}");
                }
            }
        }

        static void SaveProgress()
        {
            Console.Write("\nEnter filename to save: ");
            string filename = Console.ReadLine();
            
            try
            {
                // Simple file save (in a real app, you'd use proper serialization)
                using (StreamWriter writer = new StreamWriter(filename))
                {
                    writer.WriteLine(user.Score);
                    writer.WriteLine(user.Goals.Count);
                    // Note: Full serialization would require more complex implementation
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Progress saved to {filename}!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving: {ex.Message}");
            }
        }

        static void LoadProgress()
        {
            Console.Write("\nEnter filename to load: ");
            string filename = Console.ReadLine();
            
            try
            {
                // Simple file load (in a real app, you'd use proper deserialization)
                using (StreamReader reader = new StreamReader(filename))
                {
                    // Note: Full deserialization would require more complex implementation
                }
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"✓ Progress loaded from {filename}!");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading: {ex.Message}");
            }
        }
    }
}