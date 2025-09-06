using System;

class Program
{
    static void Main(string[] args)
    {
        bool playAgain = true;
        Random randomGenerator = new Random();

        Console.WriteLine("Guess My Number Game!");

        while (playAgain)
        {
            int magicNumber = randomGenerator.Next(1, 101);
            int guess = -1;
            int guessCount = 0;

            Console.WriteLine("\nI'm thinking of a number between 1 and 100...");

            // Loop until correct guess
            while (guess != magicNumber)
            {
                Console.Write("What is your guess? ");
                guess = int.Parse(Console.ReadLine());
                guessCount++;

                if (guess < magicNumber)
                {
                    Console.WriteLine("Higher");
                }
                else if (guess > magicNumber)
                {
                    Console.WriteLine("Lower");
                }
                else
                {
                    Console.WriteLine($"You guessed it in {guessCount} tries!");
                }
            }

            // Ask if user wants to play again
            Console.Write("Do you want to play again (yes/no)? ");
            string response = Console.ReadLine().ToLower();

            if (response != "yes")
            {
                playAgain = false;
                Console.WriteLine("Thanks for playing!");
            }
        }
    }
}
