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
        /// Best starting word for optimization.
        /// </summary>
        const string StartingWord = "rales";

        /// <summary>
        /// Entry point into the program.
        /// </summary>
        /// <param name="args">Arguments to be passed (none accepted)</param>
        static void Main(string[] args)
        {
            var allWords = new HashSet<string>(File.ReadLines(WordsPath));
            var words = new HashSet<string>(File.ReadAllLines(WordsPath));
            var guesses = 0;
            var knownLetters = new char[WordLength];
            var correctWord = string.Empty;
            var isHardMode = IsHardMode();

            while (guesses < MaxGuesses)
            {

                string guess;

                if (guesses == 0)
                {
                    guess = StartingWord;
                }
                else if (isHardMode)
                {
                    guess = GetBestGuess(words, words);
                }
                else
                {
                    guess = GetBestGuess(words, allWords);
                }
                
                if (guess == string.Empty)
                {
                    break;
                }

                if (!ValidateGuess(guess, words, allWords))
                {
                    continue;
                }

                guesses++;
                var results = new int[WordLength];

                var allCorrect = SetResults(guess, results, knownLetters);

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
        /// Gets the best guess for the puzzle based on letter frequencies. Attempts to get a guess with no repeated letters.
        /// </summary>
        /// <param name="validAnswers">The list of valid answers to the puzzle</param>
        /// <param name="validGuesses">The list of possible valid guesses</param>
        /// <returns>The best guess possible for the puzzle</returns>
        static string GetBestGuess(IEnumerable<string> validAnswers, IEnumerable<string> validGuesses)
        {
            var maxScore = int.MinValue;
            var bestGuess = string.Empty;
            if (validAnswers.Count() == 1)
            {
                foreach (var w in validAnswers)
                {
                    return w;
                }
            }

            foreach (var guess in validGuesses)
            {
                var score = 0;
                var resultToScore = new Dictionary<string, int>();
                foreach (var answer in validAnswers)
                {
                    var result = string.Empty;
                    for (int i = 0; i < answer.Length; i++)
                    {
                        if (guess[i] == answer[i])
                        {
                            result += '2';
                        }
                        else if (answer.Contains(guess[i]))
                        {
                            result += '1';
                        }
                        else
                        {
                            result += '0';
                        }
                    }

                    if (!resultToScore.ContainsKey(result))
                    {
                        var subscore = 0;
                        foreach (var word in validAnswers)
                        {
                            for (int i = 0; i < word.Length; i++)
                            {
                                if (result[i] == '2')
                                {
                                    if (word[i] != guess[i])
                                    {
                                        subscore++;
                                        break;
                                    }
                                }
                                else if (result[i] == '1')
                                {
                                    if (word[i] == guess[i] || !word.Contains(guess[i]))
                                    {
                                        subscore++;
                                        break;
                                    }
                                }
                                else
                                {
                                    if (word.Contains(guess[i]))
                                    {
                                        subscore++;
                                        break;
                                    }
                                }
                            }
                        }

                        resultToScore[result] = subscore;
                    }

                    score += resultToScore[result];
                }

                if (score > maxScore)
                {
                    maxScore = score;
                    bestGuess = guess;
                }
                else if (score == maxScore && validAnswers.Contains(guess))
                {
                    bestGuess = guess;
                }
            }

            return bestGuess;
        }

        /// <summary>
        /// Gets and sets the results of a guess based on user input
        /// </summary>
        /// <param name="guess">The guess for the puzzle</param>
        /// <param name="results">An array of ints representing the results for each letter in the guess</param>
        /// <param name="correctIndices">An array indicating which indices in the word contain the correct value</param>
        /// <returns>A value indicating whether or not all results are correct</returns>
        static bool SetResults(string guess, int[] results, char[] knownLetters)
        {
            var allCorrect = true;
            for (int i = 0; i < WordLength; i++)
            {
                if (guess[i] != knownLetters[i])
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
                        knownLetters[i] = guess[i];
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
        static bool ValidateGuess(string guess, HashSet<string> words, HashSet<string> allWords)
        {
            Console.WriteLine($"I'm guessing {guess}; is this a valid word? (y/n)");
            bool isValid = GetValidYesOrNo();

            if (!isValid)
            {
                Console.WriteLine("Got it, I'll choose something else.");
                words.Remove(guess);
                allWords.Remove(guess);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Queries the user to find out if the game is in hard mode.
        /// </summary>
        /// <returns>A value indicating whether or not the game is in hard mode</returns>
        static bool IsHardMode()
        {
            Console.WriteLine("Is this hard mode (must use result of previous guesses)? (y/n)");
            return GetValidYesOrNo();
        }

        /// <summary>
        /// Gets a value indicating whether the user said yes or no
        /// </summary>
        /// <returns>True if the user said yes, false otherwise</returns>
        static bool GetValidYesOrNo()
        {
            var answer = Console.ReadLine();

            while (answer.Length != 1 && answer[0] != 'y' && answer[0] != 'n')
            {
                Console.WriteLine("Invalid answer, please type 'y' or 'n'");
                answer = Console.ReadLine();
            }

            return answer[0] == 'y';
        }
    }
}
