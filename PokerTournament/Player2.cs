using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player2 : Player
    {
        public Player2(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
        }

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            Random rand = new Random();
            PlayerAction pa = null;
            // evaluate the hand
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            //bets the ammount based on hand rank
            int amount = 10 * rank + rand.Next(0, 15);

            //if isn't the dealer, and is going first
            if (this.Dealer == false)
            {
                if (rank == 1) //if rank is 1, no point in playing
                    pa = new PlayerAction(Name, "Bet1", "fold", amount);
                else if (rank >= 4)
                    pa = new PlayerAction(Name, "Bet1", "bet", amount);
                else // check cause the hand you ahve isn't that great
                    pa = new PlayerAction(Name, "Bet1", "check", amount);
            }
            else
            {
                if (rank == 1) //if rank is 1, no point in playing
                    pa = new PlayerAction(Name, "Bet1", "fold", amount);
                else if (rank < 5) //match the bet if hand rank is 6 or less
                    pa = new PlayerAction(Name, "Bet1", "call", amount);
                else // else raise the bet cause your hand is probably the best
                    pa = new PlayerAction(Name, "Bet1", "raise", amount);
            }

            return pa;
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            Random rand = new Random();
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            //Create the variable that will be returned
            PlayerAction pa;

            PlayerAction mostRecentAction = actions[actions.Count - 1];

            //Decision Tree

            //If the other player has already made a move
            if (actions.Count > 0)
            {
                //If the opponent checks
                if (mostRecentAction.ActionName == "check")
                {
                    //If your hand is One Pair or worse, check
                    if (rank < 2)
                        pa = new PlayerAction(Name, "Bet2", "check", 0);
                    //Your hand is better than one pair, bet a small bet to try and coax out more money from the opponent
                    else
                        pa = new PlayerAction(Name, "Bet2", "bet", rand.Next(1, 10) * rank);
                }
                //If the opponenet doesn't check
                else
                {
                    //The oppponent bet
                    if (mostRecentAction.ActionName == "bet")
                    {
                        //The bet was small (it's hard to know how people are going to bet with their AIs, I'm hoping under 50 is small)
                        if (mostRecentAction.Amount < 50)
                        {
                            //If your hand is good, raise a little bit in order to coax out more money from them
                            if (rank > 3)
                            {
                                pa = new PlayerAction(Name, "Bet2", "raise", rand.Next(20, 25));
                            }
                            //Your hand isn't good
                            else
                            {
                                //Your hand is decent, so you call that small bet
                                if (rank == 3)
                                    pa = new PlayerAction(Name, "Bet2", "call", 0); //call, so amount doesn't matter
                                //Your hand is bad, so you fold
                                else
                                {
                                    pa = new PlayerAction(Name, "Bet2", "fold", 0);
                                }
                            }
                        }
                        //The bet was not small
                        else
                        {
                            //Great hand, so you call
                            if (rank > 6)
                                pa = new PlayerAction(Name, "Bet2", "call", 0);
                            //You won't be able to compete so you fold
                            else
                                pa = new PlayerAction(Name, "Bet2", "fold", 0);
                        }
                    }
                    //The opponent did not bet or check, so they must have raised
                    else
                    {
                        //They raised a lot
                        if (mostRecentAction.Amount >= 50)
                        {
                            //Your hand is great
                            if (rank > 6)
                                pa = new PlayerAction(Name, "Bet2", "raise", rand.Next(20, 25));
                            //Your hand is not great
                            else
                            {
                                //Your hand isn't good enough
                                if (rank < 4)
                                    pa = new PlayerAction(Name, "Bet2", "fold", 0);
                                //our hand is just good enough
                                else
                                    pa = new PlayerAction(Name, "Bet2", "call", 0);
                            }
                        }
                        //They didn't raise a lot
                        else
                        {
                            //Your hand is trash
                            if (rank < 2)
                                pa = new PlayerAction(Name, "Bet2", "fold", 0);
                            //Your hand is good enough to call
                            else
                                pa = new PlayerAction(Name, "Bet2", "call", 0);
                        }

                    }
                }
            }
            //If you are the first decision
            else
            {
                //If your hand is One Pair or worse, check
                if (rank < 2)
                    pa = new PlayerAction(Name, "Bet2", "check", 0);
                //If your hand is better than one pair, bet low
                else
                    pa = new PlayerAction(Name, "Bet2", "bet", rand.Next(1, 10));
            }

            return pa;
        }

        public override PlayerAction Draw(Card[] hand)
        {

            /// Keeping it super rudimentary
            /// Fuzzy logic might be better here
            /// But since every rank corresponds with a different set of cards (IE: 1 = high card,  2 = Two pair, 10 is always = Royal Flush), 10 if statements are easier to manage imo
            /// 
             
            PlayerAction pa = new PlayerAction(Name, "Draw", "stand pat", 0);


            // Print out the hand so we can see it
            //ListTheHand(hand);
            Console.WriteLine("\n");

            // The first thing we should do is to evaluate our hand
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);


            // If you have nothing
            switch (rank)
            {
                case 1: // You have nothing; 
                    #region Section 1 (High Card)
                    Console.WriteLine("\n AI didn't like their hand.");
                    // If your high is 10 or greater, then get rid of everything but the 10+
                    // Otherwise, dump everything and redraw
                    if (highCard.Value >= 10)
                    {
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i] == highCard)
                                continue;

                            hand[i] = null;
                        }
                        pa = new PlayerAction(Name, "Draw", "draw", 4);
                        Console.WriteLine("\n AI Discarded 4 cards, and kept their high card");
                    }
                    else
                    {
                        // Dump!
                        for (int i = 0; i < hand.Length; i++)
                        {
                            hand[i] = null;
                        }
                        pa = new PlayerAction(Name, "Draw", "draw", 5);
                        Console.WriteLine("\n AI Discarded all 5 cards.");
                    }
                    #endregion
                    break;
                case 2: // We have exactly a 1 pair
                    #region Section 2 (Single Pair)

                    // First identify what number of a pair we have
                    int pairValue = 0;
                    for (int i = 2; i < 15; i++) // Loop through every possible card number
                    {
                        if (Evaluate.ValueCount(i, hand) == 2) // Thankfully we have this method
                        {
                            pairValue = i;
                            break;
                        }
                    }

                    // We know which number it is
                    // If our high card is 10 or higher, we'll want to dump every card, except for the high card and our double
                    if (highCard.Value >= 10 && highCard.Value != pairValue) // Also check to make sure our high card isn't actually our pairValue. If it is then we'll just dump everything except for the pair
                    {
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == pairValue || hand[i].Value == highCard.Value)
                                continue;

                            hand[i] = null;
                        }

                        pa = new PlayerAction(Name, "Draw", "draw", 2);
                        Console.WriteLine("\n AI has a 2 pair and has discarded everything but the 2 pair and their high card.");
                    }
                    else
                    {
                        // If our high card isn't 10 or higher, then dump every card except for the high cards
                        // Dump!
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == pairValue)
                                continue;

                            hand[i] = null;
                        }
                        pa = new PlayerAction(Name, "Draw", "draw", 3);
                        Console.WriteLine("\n AI has a 2 pair and has discarded everything but the 2 pair.");
                    }
                    #endregion
                    break;
                case 3: // We have two pairs!
                    #region Section 3 (Two Pairs)
                    // Ok first thing we need to do is to figure out which numbers are the two pair
                    int pairValue1 = 0;
                    int pairValue2 = 0;
                    for (int i = 2; i < 15; i++) // Loop through every possible card number
                    {
                        if (Evaluate.ValueCount(i, hand) == 2)
                        {
                            pairValue1 = i;
                            break;
                        }
                    }

                    // Do it again and get the second one
                    for (int i = 2; i < 15; i++) // Loop through every possible card number
                    {
                        if (Evaluate.ValueCount(i, hand) == 2)
                        {
                            if (i == pairValue2)
                                continue;
                            pairValue2 = i;
                            break;
                        }
                    }

                    if (pairValue1 == highCard.Value || pairValue2 == highCard.Value)
                    {
                        // Dump the other card and hope for a higher card
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == pairValue1 || hand[i].Value == pairValue2)
                                continue;

                            hand[i] = null;
                            pa = new PlayerAction(Name, "Draw", "draw", 1);
                        }
                    }else
                    {
                        // Keep it! Your hand is good!
                        pa = new PlayerAction(Name, "Draw", "stand pat", 0);
                    }
                    #endregion
                    break;
                case 4: // Three of a kind
                    #region Section 4 (Three of a Kind)
                    // Pretty simple. Exactly the same as 1 pair except that it's with 3
                    // First identify what number of a pair we have
                    int triValue = 0;
                    for (int i = 2; i < 15; i++) // Loop through every possible card number
                    {
                        if (Evaluate.ValueCount(i, hand) == 3) // Thankfully we have this method
                        {
                            pairValue = i;
                            break;
                        }
                    }

                    // We know which number it is
                    // If our high card is 10 or higher, we'll want to dump every card, except for the high card and our tripple
                    if (highCard.Value >= 10 && highCard.Value != triValue) // Also check to make sure our high card isn't actually our trieValue. If it is then we'll just dump everything except for the tripple
                    {
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == triValue || hand[i].Value == highCard.Value)
                                continue;

                            hand[i] = null;
                        }

                        pa = new PlayerAction(Name, "Draw", "draw", 1);
                        Console.WriteLine("\n AI has a tripple and has discarded everything but the 3 of a kind and their high card.");
                    }
                    else
                    {
                        // If our high card isn't 10 or higher, then dump every card except for the high cards
                        // Dump!
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == triValue)
                                continue;

                            hand[i] = null;
                        }
                        pa = new PlayerAction(Name, "Draw", "draw", 2);
                        Console.WriteLine("\n AI has a 3 of a kind and has discarded everything but the tripple.");
                    }
                    #endregion
                    break;
                // There's no reason for a case. Case 5 is a stroke, and we stand pat if we have a stroke
                case 8: // 4 of a kind
                    #region Section 8 (Four of a Kind)
                    // Check to see if our high is high enough. If it isn't drop it and as for another.
                    int theQuadNumber = hand[3].Value;
                    if(theQuadNumber == highCard.Value || highCard.Value <= 10)
                    {
                        // Get rid of the other card because we can do better
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == theQuadNumber)
                                continue;

                            hand[i] = null;
                        }
                        pa = new PlayerAction(Name, "Draw", "draw", 1);
                    } 
                    #endregion
                    break;
                // Any other selection is just a hold hand because we don't want to drop anything
            }


            return pa;
        }
    }
}
