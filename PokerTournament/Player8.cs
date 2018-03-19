using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    // allows a human player to participate
    class Player8 : Player
    {
        // setup your basic human player
        public Player8(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
        }

        int numCardsTossedByOpponent = 0;
        int timesRaisedBet1 = 0;
        int timesRaisedBet2 = 0;
        string amtText = "10";

        //BLUFFING
        int bluffCounter = 0;
        bool alreadyIncrementedBluffCounter = false;
        bool bluffing = false;
        int timeToBluff = 10;
        bool turnOfBluffing = false; //this becomes true if we find an AI that doesn't fall for 'stand pat' bluff

        // handle the first round of betting
        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            //increment bluff counter at start of bet1 if we haven't already
            if(!alreadyIncrementedBluffCounter)
            {
                bluffCounter++;
                alreadyIncrementedBluffCounter = true;
            }

            // list the hand
            ListTheHand(hand);

            //get rank manually
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            // select an action
            string actionSelection = "";
            PlayerAction pa = null;
            do
            {
                Console.WriteLine("Select an action:\n1 - bet\n2 - raise\n3 - call\n4 - check\n5 - fold");

                //AI input for this round
                /* Decision Tree
                 * 
                 *              BET1            |           DRAW      |                 BET2
                 *             /    \                        |                      /           \
                 *      FIRST?        SECOND                RANK                 FIRST?         SECOND?
                 *      RANK          ACTION                         CHECK THEIR DISCARD        ACTION     
                 *                    RANK                           CHECK RANK                 CHECK THEIR DISCARD
                 *                                                                              CHECK RANK
                 * 
                 */


                if(actions.Count < 1) //BET1, going first
                {
                    actionSelection = Bet1Start(rank, hand);        
                }
                else
                {
                    //get what phase we are in
                    PlayerAction lastAction = actions[actions.Count - 1];

                    if (lastAction.ActionPhase.Equals("Bet1")) //BET1 responding
                    {
                        actionSelection = Bet1(rank, hand, lastAction);
                    }
                    else if (lastAction.ActionPhase.Equals("Bet2")) //BET2, responding
                    {
                        actionSelection = Bet2(rank, hand, lastAction, actions);
                    }
                    else if (lastAction.ActionPhase.ToLower().Equals("draw")) //you are going first in Bet2
                    {
                        actionSelection = Bet2Start(rank, hand, lastAction);
                    }
                }


                // get amount if appropriate
                int amount = 0;
                if (actionSelection[0] == '1' || actionSelection[0] == '2')
                {
                    do
                    {
                        if (actionSelection[0] == '1') // bet
                        {
                            Console.Write("Amount to bet? ");
                            //amtText = Console.ReadLine();
                            //amtText = "10";
                        }
                        else if (actionSelection[0] == '2') // raise
                        {
                            Console.Write("Amount to raise? ");
                            //amtText = Console.ReadLine();
                            //amtText = "10";
                        }
                        // convert the string to an int
                        int tempAmt = 0;
                        int.TryParse(amtText, out tempAmt);

                        // check input
                        if (tempAmt > this.Money) //
                        {
                            Console.WriteLine("Amount bet is more than the amount you have available.");
                            amount = 0;
                        }
                        else if (tempAmt < 0)
                        {
                            Console.WriteLine("Amount bet or raised cannot be less than zero.");
                            amount = 0;
                        }
                        else
                        {
                            amount = tempAmt;
                        }
                    } while (amount <= 0);
                }

                // create the PlayerAction
                switch (actionSelection)
                {
                    case "1": pa = new PlayerAction(Name, "Bet1", "bet", amount); break;
                    case "2": pa = new PlayerAction(Name, "Bet1", "raise", amount); break;
                    case "3": pa = new PlayerAction(Name, "Bet1", "call", amount); break;
                    case "4": pa = new PlayerAction(Name, "Bet1", "check", amount); break;
                    case "5": pa = new PlayerAction(Name, "Bet1", "fold", amount); break;
                    default: Console.WriteLine("Invalid menu selection - try again"); continue;
                }
            } while (actionSelection != "1" && actionSelection != "2" &&
                    actionSelection != "3" && actionSelection != "4" &&
                    actionSelection != "5");
            // return the player action
            return pa;
        }

        // reuse the same logic for second betting round
        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {

            PlayerAction pa1 = BettingRound1(actions, hand);

            // create a new PlayerAction object
            return new PlayerAction(pa1.Name, "Bet2", pa1.ActionName, pa1.Amount);
        }

        public override PlayerAction Draw(Card[] hand)
        {
            //reset the timesRaised variable after Bet1 is done
            timesRaisedBet1 = 0;
            timesRaisedBet2 = 0;

            //increment bluffCounter in draw because we only draw once every round (unlike betting that has
            //multiple phases
            alreadyIncrementedBluffCounter = false;
            bluffing = false;

            // list the hand
            ListTheHand(hand);

            //card index to deletes
            List<int> deleteIndexes = new List<int>();

            //get rank manually
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            // determine how many cards to delete
            int cardsToDelete = 0;
            do
            {
                Console.Write("How many cards to delete? "); // get the count
                //string deleteStr = Console.ReadLine();
                string deleteStr;

                //use a decision tree to figure out the cards to delete
                deleteStr = "0";
                //straight, flush, full house, straight flush, or royal flush
                if(rank == 5 || rank == 6 || rank == 7 || rank == 9 || rank == 10)
                {
                    deleteStr = "0";
                }

                //4 of a kind
                if(rank == 8)
                {
                    //throw out 1 unless it's a King or Ace
                    Dictionary<int, int> handMap = new Dictionary<int, int>();
                    for(int i = 0; i < hand.Length; i++)
                    {
                        if(handMap.ContainsKey(hand[i].Value))
                        {
                            handMap[hand[i].Value]++;
                        }
                        else
                        {
                            handMap.Add(hand[i].Value, 1);
                        }
                    }

                    int v = handMap.FirstOrDefault(x => x.Value == 1).Key;
                    if(v < 14) //if less than ace
                    {
                        deleteStr = "1";
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == v)
                            {
                                deleteIndexes.Add(i);
                            }
                        }
                    }
                    else
                    {
                        deleteStr = "0";
                    }
                }

                //3 of a kind
                if(rank == 4)
                {
                    //throw out 2 that are not part of 3 of kind
                    Dictionary<int, int> handMap = new Dictionary<int, int>();
                    for (int i = 0; i < hand.Length; i++)
                    {
                        if (handMap.ContainsKey(hand[i].Value))
                        {
                            handMap[hand[i].Value]++;
                        }
                        else
                        {
                            handMap.Add(hand[i].Value, 1);
                        }
                    }

                    int v = handMap.FirstOrDefault(x => x.Value == 3).Key; //3 kind value

                    for(int i = 0; i < hand.Length; i++)
                    {
                        if(hand[i].Value != v)
                        {
                            deleteIndexes.Add(i);
                        }
                    }

                    deleteStr = "2";
                }

                //2 pair
                if(rank == 3)
                {
                    //throw out 1 card not in either pair
                    Dictionary<int, int> handMap = new Dictionary<int, int>();
                    for (int i = 0; i < hand.Length; i++)
                    {
                        if (handMap.ContainsKey(hand[i].Value))
                        {
                            handMap[hand[i].Value]++;
                        }
                        else
                        {
                            handMap.Add(hand[i].Value, 1);
                        }
                    }

                    int v = handMap.FirstOrDefault(x => x.Value == 1).Key;
                    if (v < 14) //if less than ace
                    {
                        deleteStr = "1";
                        for (int i = 0; i < hand.Length; i++)
                        {
                            if (hand[i].Value == v)
                            {
                                deleteIndexes.Add(i);
                            }
                        }
                    }
                    else
                    {
                        deleteStr = "0";
                    }
                }

                //1 pair
                if(rank == 2)
                {
                    //throw out 3 cards not in the pair
                    Dictionary<int, int> handMap = new Dictionary<int, int>();
                    for (int i = 0; i < hand.Length; i++)
                    {
                        if (handMap.ContainsKey(hand[i].Value))
                        {
                            handMap[hand[i].Value]++;
                        }
                        else
                        {
                            handMap.Add(hand[i].Value, 1);
                        }
                    }

                    int v = handMap.FirstOrDefault(x => x.Value == 2).Key;

                    for(int i = 0; i < hand.Length; i++)
                    {
                        if(hand[i].Value != v)
                        {
                            deleteIndexes.Add(i);
                        }
                    }

                    deleteStr = "3";
                }

                //nothing - throw out 4 lowest
                if (rank <= 1)
                {
                    if (bluffCounter > timeToBluff && turnOfBluffing == false)
                    {
                        deleteStr = "0";
                        bluffing = true;
                        bluffCounter = 0;
                        //Console.WriteLine("bluffing");
                        //Console.ReadLine();
                    }
                    else
                    {
                        deleteStr = "4";
                        for (int i = 0; i < 4; i++)
                        {
                            deleteIndexes.Add(i);
                        }
                    }
                }


                    int.TryParse(deleteStr, out cardsToDelete);

            } while (cardsToDelete < 0 || cardsToDelete > 5);

            // which cards to delete if any
            PlayerAction pa = null;
            if (cardsToDelete > 0 && cardsToDelete < 5)
            {
                for (int i = 0; i < cardsToDelete; i++) // loop to delete cards
                {
                    Console.WriteLine("\nDelete card " + (i + 1) + ":");
                    for (int j = 0; j < hand.Length; j++)
                    {
                        Console.WriteLine("{0} - {1}", (j + 1), hand[j]);
                    }
                    // selete cards to delete
                    int delete = 0;
                    do
                    {

                        Console.Write("Which card to delete? (1 - 5): ");
                        //string delStr = Console.ReadLine();
                        //int.TryParse(delStr, out delete);

                        delete = deleteIndexes[i] + 1;

                        // see if the entry is valid
                        if (delete < 1 || delete > 5)
                        {
                            Console.WriteLine("Invalid entry - enter a value between 1 and 5.");
                            delete = 0;
                        }
                        else if (hand[delete - 1] == null)
                        {
                            Console.WriteLine("Entry was already deleted.");
                            delete = 0;
                        }
                        else
                        {
                            hand[delete - 1] = null; // delete entry
                            delete = 99; // flag to exit loop
                        }
                    } while (delete == 0);
                }
                // set the PlayerAction object
                pa = new PlayerAction(Name, "draw", "draw", cardsToDelete);
            }
            else if (cardsToDelete == 5)
            {
                // delete them all
                for (int i = 0; i < hand.Length; i++)
                {
                    hand[i] = null;
                }
                pa = new PlayerAction(Name, "draw", "draw", 5);
            }
            else // no cards deleted
            {
                pa = new PlayerAction(Name, "draw", "stand pat", 0);
            }

            // return the action
            return pa;
        }

        // helper method - list the hand
        private void ListTheHand(Card[] hand)
        {
            // evaluate the hand
            Card highCard = null;
            int rank = Evaluate.RateAHand(hand, out highCard);

            // list your hand
            Console.WriteLine("\nName: " + Name + " Your hand:   Rank: " + rank);
            for (int i = 0; i < hand.Length; i++)
            {
                Console.Write(hand[i].ToString() + " ");
            }
            Console.WriteLine();
        }

        //helper method to handle the decision in Bet1 phase - GOING FIRST
        private string Bet1Start(int rank, Card[] hand)
        {
            //Console.WriteLine("Going FIRST");
            if (rank <= 1) //bad hand, just check
            {
                return "4";
            }

            //if pair and its AA, limp in - check, this baits them into raising even if they have standard pair
            else if (rank == 2)
            {
                Dictionary<int, int> handMap = new Dictionary<int, int>();
                for (int i = 0; i < hand.Length; i++)
                {
                    if (handMap.ContainsKey(hand[i].Value))
                    {
                        handMap[hand[i].Value]++;
                    }
                    else
                    {
                        handMap.Add(hand[i].Value, 1);
                    }
                }

                int v = handMap.FirstOrDefault(x => x.Value == 2).Key;

                if (v == 14)
                {
                    return "4";
                }
                else
                {
                    amtText = "10";
                    return "1";
                }
            }
            else //better than pair
            {
                amtText = "20";
                return "1";
            }
        }

        //helper method to handle the decision in Bet1 phase
        private string Bet1(int rank, Card[] hand, PlayerAction lastAction)
        {
            //Console.WriteLine("OPPONENTS LAST ACTION: " + lastAction.ActionName);

            if (lastAction.ActionName.Equals("check"))
            {
                if (rank <= 1) //bad hand, let's check as well
                {
                    return "4";
                }
                else //always raise in
                {
                    amtText = "10";
                    return "1";
                }
            }
            else if (lastAction.ActionName.Equals("bet"))
            {
                if (rank <= 1) //bad hand, fold
                {
                    return "5";
                }
                else //always raise, never call
                {
                    if (timesRaisedBet1 < 1)
                    {
                        amtText = "10";
                        timesRaisedBet1++;
                        return "2";
                    }
                    else
                    {
                        return "3";
                    }
                }
            }
            else if (lastAction.ActionName.Equals("raise"))
            {
                //if they raised us, we must have already bet so ignore rechecking hands
                return "3";
            }
            else if (lastAction.ActionName.Equals("call")) //should never get here
            {
                //if they raised us, we must have already bet so ignore bad hands
                return "3";
            }
            else //otherwise check - should never get here
            {
                return "4";
            }
        }

        //helper method to handle the decision in Bet2 phase - GOING FIRST
        private string Bet2Start(int rank, Card[] hand, PlayerAction lastAction)
        {
            //bluff
            if (bluffing)
            {
                amtText = "10";
                return "1";
            }
            else if (rank <= 1) //bad hand, just check
            {
                return "4";
            }
            else if (lastAction.ActionName.Equals("stand pat")) //nothing
            {
                //probably fold, however they may be bluffing
                return "4";
            }
            else //get # of cards they threw away
            {
                int cardsTossed = lastAction.Amount;
                if (cardsTossed >= 4) //bad hand, bet
                {
                    amtText = "10";
                    return "1";
                }
                if (cardsTossed == 3) //they had a pair
                {
                    if (rank > 2) //pair or better
                    {
                        amtText = "10";
                        if(rank >= 4)
                        {
                            amtText = "30";
                        }
                        return "1";
                    }
                    else
                    {
                        return "4";
                    }
                }
                if (cardsTossed == 2) //they had a triple
                {
                    if (rank > 4)
                    {
                        amtText = "10";
                        if (rank >= 5)
                        {
                            amtText = "30";
                        }
                        return "1";
                    }
                    else
                    {
                        return "4";
                    }
                }
                if (cardsTossed == 1) //had a two pair
                {
                    if (rank > 3)
                    {
                        if (rank >= 4)
                        {
                            amtText = "30";
                        }
                        return "1";
                    }
                    else
                    {
                        return "4";
                    }
                }

                return "4"; //never hits this, check if it does
            }
        }

        //helper method to handle the decision in Bet2 phase
        private string Bet2(int rank, Card[] hand, PlayerAction lastAction, List<PlayerAction> actions)
        {
            //update the cards our opponent threw away if applicable...get three turns ago to check if it was a draw phase
            PlayerAction drawAction = actions[actions.Count - 3];
            if (drawAction.ActionPhase.ToLower().Equals("draw"))
            {
                //Console.WriteLine("OPPONENT DREW CARDS THREE TURNS AGO");
                //Console.WriteLine("CARDS TOSSED: " + drawAction.Amount);
                if (drawAction.ActionName.Equals("stand pat"))
                {
                    numCardsTossedByOpponent = 0;
                }
                else
                {
                    numCardsTossedByOpponent = drawAction.Amount;
                }
            }

            if (lastAction.ActionName.Equals("check"))
            {
                if (bluffing)
                {
                    amtText = "10";
                    return "1";
                }
                else if (rank <= 1) //bad hand, let's check as well
                {
                    return "4";
                }
                else
                {
                    if (numCardsTossedByOpponent == 0)
                    {
                        return "4";
                    }
                    else if (numCardsTossedByOpponent == 1) //they had two pair
                    {
                        if (rank > 3)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            return "1";
                        }
                        else
                        {
                            return "4";
                        }
                    }
                    else if (numCardsTossedByOpponent == 2) //they had triple
                    {
                        if (rank >= 4)
                        {
                            amtText = "10";
                            if (rank >= 5)
                            {
                                amtText = "30";
                            }
                            return "1";
                        }
                        else
                        {
                            return "4";
                        }
                    }
                    else if (numCardsTossedByOpponent == 3) //they had a pair
                    {
                        if (rank >= 2)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            return "1";
                        }
                        else
                        {
                            return "4";
                        }
                    }
                    else //they threw away 4 cards
                    {
                        return "1";
                    }
                }
            }
            else if (lastAction.ActionName.Equals("bet"))
            {
                if (bluffing) //if they bet us after we discarded
                {
                    turnOfBluffing = true;
                    return "5";
                }
                else if (rank <= 1) //bad hand, fold
                {
                    return "5";
                }
                if (numCardsTossedByOpponent == 0) //they stood pat
                {
                    if (rank == 5 || rank == 6) //straight or flush we call
                    {
                        return "3";
                    }
                    else if (rank >= 7) //full house or better we raise
                    {
                        if (timesRaisedBet2 < 2)
                        {
                            amtText = "10";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than twice
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 1) //two pair
                {
                    if (rank == 3)
                    {
                        return "3";
                    }
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 2) //triple
                {
                    if (rank == 4)
                    {
                        return "3";
                    }
                    else if (rank >= 5)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 5)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 3) //pair
                {
                    if (rank == 2)
                    {
                        return "3";
                    }
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else //they threw away 4 cards
                {
                    if (rank == 2)
                    {
                        return "3";
                    }
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 3)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "3";
                    }
                }
            }
            else if (lastAction.ActionName.Equals("raise"))
            {
                if (bluffing) //they raised our bet when doing a stand pat bluff, turn off bluffing
                {
                    turnOfBluffing = true;
                    return "5";
                }
                else if (rank <= 1) //bad hand, fold
                {
                    return "5";
                }
                else if (numCardsTossedByOpponent == 0) //they stood pat
                {
                    if (rank == 5 || rank == 6) //straight or flush we call
                    {
                        return "3";
                    }
                    else if (rank >= 7) //full house or better we raise
                    {
                        if (timesRaisedBet2 < 2)
                        {
                            amtText = "10";
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 1) //two pair
                {
                    if (rank == 3)
                    {
                        return "3";
                    }
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 2) //triple
                {
                    if (rank == 4)
                    {
                        return "3";
                    }
                    else if (rank >= 5)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 5)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else if (numCardsTossedByOpponent == 3) //pair
                {
                    if (rank == 2 || rank == 3)
                    {
                        return "3";
                    }
                    else if (rank >= 4)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 4)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "5";
                    }
                }
                else //they threw away 4 cards
                {
                    if (rank == 2)
                    {
                        return "3";
                    }
                    else if (rank >= 3)
                    {
                        if (timesRaisedBet2 < 1)
                        {
                            amtText = "10";
                            if (rank >= 3)
                            {
                                amtText = "30";
                            }
                            timesRaisedBet2++;
                            return "2";
                        }
                        else //don't raise more than once
                        {
                            return "3";
                        }
                    }
                    else
                    {
                        return "3";
                    }
                }
            }
            else if (lastAction.ActionName.Equals("call"))
            {
                //if they called us, we must have already bet so ignore bad hands
                return "3";
            }

            //otherwise check - should never get here
            else
            {

                return "4";
            }
        }
    }
}
