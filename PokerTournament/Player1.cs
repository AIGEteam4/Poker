using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    // Quick note - this bot may take a few seconds to decide on an action, especially during the draw phase.
    //
    // Explanation of each phase:
    //
    // //// Betting1 - TODO ////
    //
    // //// Draw ////
    // The draw phase is a brute-force method based on probabilities of scores exceeding the current hand.
    // We use two heuristics - first is the value of a hand (in this case, tEvaluate.RateAHand * 100 + highCard.Value).
    // The second heuristic is the probability that a given hand will happen based on our current hand.
    //
    // For each possible action (discard) we can take during the draw phase, we check every possible resulting hand and add up the possible
    // scores, then multiply that with the probability that hand will happen. The result is the score for that action. We then take the action
    // with the highest score.
    //
    // For optimization purposes, we also don't test any actions that discard 4 or more cards, as at that point the likelihood we will get a decent
    // hand is low, so we should have already folded. If we are attempting to bluff, then the action we take doesn't really matter and discarding
    // 4+ cards would tell the other player that we have a bad hand anyway.
    //
    // //// Betting2 - TODO ////
    class Player1 : Player
    {
        // Score is Evaluate.RateAHand * 100 + highCard.value.
        private static double ScoreHand(Card[] hand)
        {
            // for now, we just pass this to evaluate
            Card highCard;
            return (double)(Evaluate.RateAHand(hand, out highCard) * 100 + highCard.Value);
        }

        private List<int[]> possibleActions = new List<int[]>();
        private string[] suits = { "Hearts", "Clubs", "Diamonds", "Spades" };
        private int[] chosenDiscardAction = null;
        private double predictedScore = 0.0;

        public Player1(int idNum, string nm, int mny)
            : base(idNum, nm, mny)
        {
            // All possible ways we can discard cards.
            // This isn't generated as we might want to remove some instances in the name of
            // optimization.
            // Each action is a list of indices that should be redrawn.
            possibleActions.Add(new int[] { }); // don't discard anything

            possibleActions.Add(new int[] { 0 }); // discard one
            possibleActions.Add(new int[] { 1 });
            possibleActions.Add(new int[] { 2 });
            possibleActions.Add(new int[] { 3 });
            possibleActions.Add(new int[] { 4 });

            possibleActions.Add(new int[] { 0, 1 }); // discard two
            possibleActions.Add(new int[] { 0, 2 });
            possibleActions.Add(new int[] { 0, 3 });
            possibleActions.Add(new int[] { 0, 4 });
            possibleActions.Add(new int[] { 1, 2 });
            possibleActions.Add(new int[] { 1, 3 });
            possibleActions.Add(new int[] { 1, 4 });
            possibleActions.Add(new int[] { 2, 3 });
            possibleActions.Add(new int[] { 2, 4 });
            possibleActions.Add(new int[] { 3, 4 });

            possibleActions.Add(new int[] { 0, 1, 2 }); // discard three
            possibleActions.Add(new int[] { 0, 1, 3 });
            possibleActions.Add(new int[] { 0, 1, 4 });
            possibleActions.Add(new int[] { 0, 2, 3 });
            possibleActions.Add(new int[] { 0, 2, 4 });
            possibleActions.Add(new int[] { 0, 3, 4 });

            possibleActions.Add(new int[] { 1, 2, 3 });
            possibleActions.Add(new int[] { 1, 2, 4 });
            possibleActions.Add(new int[] { 1, 3, 4 });

            possibleActions.Add(new int[] { 2, 3, 4 });
            
            // It generally isn't worth checking these options.
            /*
            possibleActions.Add(new int[] { 0, 1, 2, 3 }); // discard four
            possibleActions.Add(new int[] { 0, 1, 2, 4 });
            possibleActions.Add(new int[] { 0, 1, 3, 4 });
            possibleActions.Add(new int[] { 0, 2, 3, 4 });
            possibleActions.Add(new int[] { 1, 2, 3, 4 });

            possibleActions.Add(new int[] { 0, 1, 2, 3, 4 }); // discard five
            */

            // possibleActions should be sorted shortest discard to longest.
            possibleActions = possibleActions.OrderBy(discard => discard.Length).ToList();
        }

        private Card BuildTestCard(int index)
        {
            string s = "";
            int v = 0;
            int idx = 0;
            foreach (var suit in suits)
            {
                s = suit;
                for (var value = 2; value <= 14; ++value)
                {
                    v = value;
                    ++idx;

                    if (idx >= index)
                        break;
                }

                if (idx >= index)
                    break;
            }

            return new Card(s, v);
        }

        // Recursively score possible hands.
        private double ScorePossibleHand(Card[] starting, Card[] discarded)
        {
            int firstNullIndex = -1;
            Card[] test = new Card[5];
            for (var i = 0; i < 5; ++i)
            {
                if (starting[i] == null && firstNullIndex == -1)
                {
                    firstNullIndex = i;
                }

                test[i] = starting[i];
            }

            if (firstNullIndex == -1)
            {
                return ScoreHand(test);
            }
            else
            {
                double totalScore = 0.0;
                foreach (var suit in suits)
                {
                    for (var i = 2; i <= 14; ++i)
                    {
                        bool alreadyDiscarded = false;
                        foreach (var c in discarded)
                        {
                            if (c.Suit == suit && c.Value == i)
                            {
                                alreadyDiscarded = true;
                                break;
                            }
                        }

                        if (alreadyDiscarded)
                            continue;

                        foreach (var c in test)
                        {
                            if (c != null && c.Suit == suit && c.Value == i)
                            {
                                alreadyDiscarded = true;
                                break;
                            }
                        }

                        if (alreadyDiscarded)
                            continue;

                        test[firstNullIndex] = new Card(suit, i);
                        totalScore += ScorePossibleHand(test, discarded);
                    }
                }

                return totalScore;
            }
        }

        // For each possible result of the given discard, score the resulting hand * the probability of that hand.
        // Add all scores together and you get the score of the discard action.
        private double ScoreDiscard(Card[] hand, int[] discards)
        {
            if (discards.Length == 0)
                return ScoreHand(hand);

            Card[] testHand = new Card[5];
            List<Card> discarded = new List<Card>();
            for (var i = 0; i < 5; ++i)
            {
                if (!discards.Contains(i))
                    testHand[i] = hand[i];
                else
                {
                    testHand[i] = null;
                    discarded.Add(hand[i]);
                }
            }

            double score = ScorePossibleHand(testHand, discarded.ToArray());
            double eachProbability = Math.Pow(1.0 / (52.0 - 5.0), discards.Length); // this heuristic is slightly inaccurate, as it doesn't account for discarded cards or what we currently have in our hand.

            score *= eachProbability;
            return score;
        }

        private int[] FindBestDiscard(Card[] hand, out double predictedScore)
        {
            bool debugMode = true;

            double bestScore = 0.0;
            int[] bestDiscard = null;

            if (!debugMode)
                Console.Write("Thinking");

            foreach (var discard in possibleActions)
            {
                if (debugMode)
                {
                    Console.WriteLine(string.Format("Discard: {0}", string.Join(",", discard)));
                }

                double score = ScoreDiscard(hand, discard);

                if (debugMode)
                {
                    Console.WriteLine(string.Format("Score: {0}", score));
                }
                else
                {
                    Console.Write(".");
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDiscard = discard;
                }
            }

            if (debugMode)
            {
                Console.WriteLine("BEST RESULT:");
                Console.WriteLine(string.Format("Discard: {0}", string.Join(",", bestDiscard)));
                Console.WriteLine(string.Format("Score: {0}", bestScore));
            }
            else
            {
                Console.WriteLine(" Done!");
            }

            predictedScore = bestScore;
            return bestDiscard;
        }

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            // We bet based on what we predict our hand score will be, not what our current score is.
            if (actions.Count <= 1)
            {
                chosenDiscardAction = FindBestDiscard(hand, out predictedScore);
            }

            //Evaluate.SortHand(hand);

            if (predictedScore < 200)
            {
                if (actions.Count > 0)
                {
                    if (actions.Last<PlayerAction>().Name == "check")
                    {
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    }
                    else
                        return new PlayerAction(Name, "Bet1", "fold", 0);
                }
            }
            else
            {
                int desiredBet =(int)((predictedScore - 199) / 1.5);

                if (actions.Count == 0)
                {
                    return new PlayerAction(Name, "Bet1", "bet", desiredBet);
                }
                else if (actions.Count == 1)
                {
                    if (actions.Last<PlayerAction>().ActionName == "check")
                    {
                        return new PlayerAction(Name, "Bet1", "bet", desiredBet);
                    }
                    if (actions.Last<PlayerAction>().ActionName == "bet")
                    {
                        if (actions.Last<PlayerAction>().Amount < desiredBet)
                        {
                            return new PlayerAction(Name, "Bet1", "bet", desiredBet);

                        }
                        else if (actions.Last().Amount == desiredBet)
                        {
                            return new PlayerAction(Name, "Bet1", "call",desiredBet);
                        }
                        else
                        {
                            return new PlayerAction(Name, "Bet1", "fold", 0);
                        }
                    }

                }
                else if (actions.Count >= 2)
                {
                    int opponentHand =(int)((actions.Last().Amount*1.5)+199);
                    if (opponentHand + 50 > predictedScore)
                    {
                        return new PlayerAction(Name, "Bet1", "fold", 0);

                    }
                    else
                    {
                        return new PlayerAction(Name, "Bet1", "call", actions.Last().Amount);

                    }
                }

            }

            return new PlayerAction(Name, "Bet1", "check", 0);
        }

        // The draw phase is a bit of a brute-force method.
        // We build a table of probabilities and scores (technically we don't actually build this table - we just store the best result as we go along)
        // based on possible actions we can take. For each possible action we take, we add up the scores of all possible hands we can get after
        // the action and then multipliy that by a heuristic based on the probability for each action (in this case, it's (1/52)^(numberOfCardsDiscarded)).
        // The action with the highest score is the one we choose.
        // We also ignore any actions discarding more than 3 cards, as it gets VERY slow and the likelihood of getting any good hands by discarding 4+ cards
        // is very low. If our initial hand is so bad that a 4+ discard makes sense, we should have folded or bet low in the first betting round.
        public override PlayerAction Draw(Card[] hand)
        {
            foreach (var index in chosenDiscardAction)
            {
                hand[index] = null;
            }

            return new PlayerAction(Name, "Draw", "draw", chosenDiscardAction.Length);
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            double handScore = ScoreHand(hand);

            if (handScore < 200)
            {
                if (actions.Count > 0)
                {
                    if (actions.Last<PlayerAction>().Name == "check")
                    {
                        return new PlayerAction(Name, "Bet2", "check", 0);
                    }
                    else
                        return new PlayerAction(Name, "Bet2", "fold", 0);
                }
            }
            else
            {
                int desiredBet = (int)((handScore - 199) / 1.5);

                if (actions.Count == 0)
                {
                    return new PlayerAction(Name, "Bet2", "bet", desiredBet);
                }
                else if (actions.Count == 1)
                {
                    if (actions.Last<PlayerAction>().ActionName == "check")
                    {
                        return new PlayerAction(Name, "Bet2", "bet", desiredBet);
                    }
                    if (actions.Last<PlayerAction>().ActionName == "bet")
                    {
                        if (actions.Last<PlayerAction>().Amount < desiredBet)
                        {
                            return new PlayerAction(Name, "Bet2", "bet", desiredBet);

                        }
                        else if (actions.Last().Amount == desiredBet)
                        {
                            return new PlayerAction(Name, "Bet2", "call", desiredBet);
                        }
                        else
                        {
                            return new PlayerAction(Name, "Bet2", "fold", 0);
                        }
                    }

                }
                else if (actions.Count >= 2)
                {
                    int opponentHand = (int)((actions.Last().Amount * 1.5) + 199);
                    if (opponentHand + 50 > handScore)
                    {
                        return new PlayerAction(Name, "Bet2", "fold", 0);

                    }
                    else
                    {
                        return new PlayerAction(Name, "Bet2", "call", actions.Last().Amount);

                    }
                }

            }

            return new PlayerAction(Name, "Bet2", "check", 0);
        }
    }
}
