namespace Wordle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// A class that is used to solve Wordle puzzles
    /// </summary>
    class WordleSolverMain
    {
        /// <summary>
        /// Path to the file containing all possible 5-letter English words.
        /// </summary>
        const string WordsPath = @"C:\Users\reill_dkquiaq\source\repos\Wordle\Wordle\words.txt";

        /// <summary>
        /// Maximum number of guesses before failing the Wordle.
        /// </summary>
        const int MaxGuesses = 6;

        /// <summary>
        /// Length of the words being guessed.
        /// </summary>
        const int WordLength = 5;

        /// <summary>
        /// Instructions displayed to the end-user for letter input.
        /// </summary>
        const string LetterCheckInstructions = "(0: wrong letter, 1: right letter wrong spot, 2: right letter right spot)";

        /// <summary>
        /// Entry point into the program.
        /// </summary>
        /// <param name="args">Arguments to be passed (none accepted)</param>
        static void Main(string[] args)
        {
            var letterFrequencies = new Dictionary<char, int>();
            var words = new HashSet<string>(File.ReadAllLines(WordsPath));
            var guesses = 0;
            var correctIndices = new bool[WordLength];
            var correctWord = string.Empty;

            while (guesses < MaxGuesses)
            {
                SetLetterFrequencies(letterFrequencies, words);

                var guess = GetBestGuess(letterFrequencies, words);
                if (guess == string.Empty)
                {
                    break;
                }

                if (!ValidateGuess(guess, words))
                {
                    continue;
                }

                guesses++;
                var results = new int[WordLength];
                var allCorrect = SetResults(guess, results, correctIndices);

                if (allCorrect)
                {
                    correctWord = guess;
                    break;
                }

                words = GetPrunedWords(guess, words, results);
            }

            if (correctWord == string.Empty)
            {
                Console.Write("Shit, I couldn't get it... Sorry, bub :(");
            }
            else
            {
                Console.WriteLine($"Sweet, I got it in {guesses} guesses! The word is {correctWord}.");
            }

            Console.Read();
        }

        /// <summary>
        /// Sets up the letter frequency dictionary based on the given list of words.
        /// </summary>
        /// <param name="letterFrequencies">A dictionary that maps a given character to the number of times it appears in a list of words</param>
        /// <param name="words">The list of words</param>
        static void SetLetterFrequencies(IDictionary<char, int> letterFrequencies, IEnumerable<string> words)
        {
            for (char c = 'a'; c <= 'z'; c++)
            {
                letterFrequencies[c] = 0;
            }

            foreach (var word in words)
            {
                foreach (var c in word)
                {
                    letterFrequencies[c]++;
                }
            }
        }

        /// <summary>
        /// Gets the best guess for the puzzle based on letter frequencies. Attempts to get a guess with no repeated letters.
        /// </summary>
        /// <param name="letterFrequencies">A dictionary that maps a given character to the number of times it appears in a list of words</param>
        /// <param name="words">The list of words</param>
        /// <returns>The best guess possible for the puzzle</returns>
        static string GetBestGuess(IDictionary<char, int> letterFrequencies, IEnumerable<string> words)
        {
            var maxScore = int.MinValue;
            var maxScoreNoRepeats = int.MinValue;
            var bestGuess = string.Empty;
            var bestGuessNoRepeats = string.Empty;
            foreach (var word in words)
            {
                var score = 0;
                var repeats = false;
                var seen = new HashSet<char>();
                foreach (var c in word)
                {
                    score += letterFrequencies[c];
                    if (seen.Contains(c))
                    {
                        repeats = true;
                    }
                    else
                    {
                        seen.Add(c);
                    }
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestGuess = word;
                }

                if (score > maxScoreNoRepeats && !repeats)
                {
                    maxScoreNoRepeats = score;
                    bestGuessNoRepeats = word;
                }
            }

            if (bestGuessNoRepeats == string.Empty)
            {
                return bestGuess;
            }

            return bestGuessNoRepeats;
        }

        /// <summary>
        /// Gets and sets the results of a guess based on user input
        /// </summary>
        /// <param name="guess">The guess for the puzzle</param>
        /// <param name="results">An array of ints representing the results for each letter in the guess</param>
        /// <param name="correctIndices">An array indicating which indices in the word contain the correct value</param>
        /// <returns>A value indicating whether or not all results are correct</returns>
        static bool SetResults(string guess, int[] results, bool[] correctIndices)
        {
            var allCorrect = true;
            for (int i = 0; i < WordLength; i++)
            {
                if (!correctIndices[i])
                {
                    Console.Write($"My guess is {guess}; is {guess[i]} in position {i} correct? {LetterCheckInstructions}");
                    var answer = Console.ReadLine();
                    while (answer.Length != 1 && answer[0] != '0' && answer[0] != '1' && answer[0] != '2')
                    {
                        Console.Write($"Invalid response, please provide valid input. {LetterCheckInstructions}");
                        answer = Console.ReadLine();
                    }

                    var result = answer[0] - '0';
                    results[i] = result;
                    if (result == 2)
                    {
                        correctIndices[i] = true;
                    }
                    else
                    {
                        allCorrect = false;
                    }
                }
                else
                {
                    results[i] = 2;
                }
            }

            return allCorrect;
        }

        /// <summary>
        /// Gets a new list of words with invalid words pruned out based on the results of the guess.
        /// </summary>
        /// <param name="guess">The guess provided</param>
        /// <param name="words">The list of words to be pruned</param>
        /// <param name="results">The results of the guess provided</param>
        /// <returns>A new set of words with invalid entries pruned out</returns>
        static HashSet<string> GetPrunedWords(string guess, IEnumerable<string> words, int[] results)
        {
            var result = new HashSet<string>();
            foreach (var word in words)
            {
                var match = true;
                for (int i = 0; i < word.Length; i++)
                {
                    if (results[i] == 2 && word[i] != guess[i])
                    {
                        match = false;
                        break;
                    }

                    if (results[i] == 1 && (word[i] == guess[i] || !word.Contains(guess[i])))
                    {
                        match = false;
                        break;
                    }

                    if (results[i] == 0 && word.Contains(guess[i]))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    result.Add(word);
                }
            }
            
            return result;
        }

        /// <summary>
        /// Validates a given guess based on user input and removes the word from the set of words if it's invalid.
        /// </summary>
        /// <param name="guess">The guess to be validated</param>
        /// <param name="words">The set of words to remove invalid guesses from</param>
        /// <returns>A value indicating whether or not the guess was valid</returns>
        static bool ValidateGuess(string guess, HashSet<string> words)
        {
            Console.WriteLine($"I'm guessing {guess}; is this a valid word? (y/n)");
            var answer = Console.ReadLine();

            while (answer.Length != 1 && answer[0] != 'y' && answer[0] != 'n')
            {
                Console.WriteLine("Invalid answer, please type 'y' or 'n'");
                answer = Console.ReadLine();
            }

            if (answer[0] == 'n')
            {
                Console.WriteLine("Got it, I'll choose something else.");
                words.Remove(guess);
                return false;
            }

            return true;
        }
    }
}
