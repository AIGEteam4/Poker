using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player4 : Player
    {
        Random rand;//Random number generator to occasionally randomly determine an outcome

        int opponentMoney;//Keeps track of how much money the opponent has

        int numTimesRaised;//Keep track of number of times we've raised

        bool isBluffing;
        int bluffChance;

        Round currentRound;//Object keeps track of important info about this round
       

        float[] handProbability = {
            0.5f,//0 - high card, 50% chance of occurring
            0.58f,//1 - one pair, 42% chance of occurring
            0.95f,//2 - two pair, ~5% chance of occurring
            0.98f,//3 - three of a kind, ~2% chance of occurring
            1//4+ - straight or better, <1% chance of occurring
        };

        public Player4(int idNum, string nm, int mny) : base(idNum, nm, mny)
        {
            rand = new Random();//Rand object for any random number generation we need to do

            currentRound = new Round();

            opponentMoney = mny;//Assume they start with the same amt of money as us

            isBluffing = false;
            bluffChance = 0;
        }

        //Start a new round
        private void NewRound()
        {
            currentRound.AdvanceRound();
            isBluffing = false;
            numTimesRaised = 0;
        }

        //Actions for betting round 1
        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            int round1Result;

            //If they're going first, get their action so we can factor that in
            //Can call, raise, or fold
            if (actions.Count > 0)
            {
                if(actions.Count <= 1)
                {
                    NewRound();
                }

                PlayerAction opponentAction = actions.Last();

                string actionName = opponentAction.ActionName.ToLower();

                //They bet or raised
                if (actionName.Equals("bet") || actionName.Equals("raise"))
                {
                    currentRound.AddOpponentBet(opponentAction.Amount);

                    round1Result = GetRound1Action(opponentAction.Amount);

                    if (round1Result == 0)
                        return new PlayerAction(Name, "Bet1", "call", 0);
                    else if (round1Result > 0)
                    {
                        currentRound.AddPlayerBet(round1Result);
                        return new PlayerAction(Name, "Bet1", "raise", round1Result);
                    }
                        
                }
                //They checked
                else
                {
                    round1Result = GetRound1Action(0);

                    if (round1Result == 0)
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    else if (round1Result > 0)
                    {
                        currentRound.AddPlayerBet(round1Result);
                        return new PlayerAction(Name, "Bet1", "bet", round1Result);
                    }
                }

                //If reached this point, fold
                return new PlayerAction(Name, "Bet1", "fold", 0);
            }
            //If we're going first, will have to base decision entirely off of hand strength
            //Can check, bet, or fold
            else
            {
                NewRound();//Start new round

                round1Result = GetRound1Action(-1);

                if (round1Result < 0)
                    return new PlayerAction(Name, "Bet1", "fold", 0);
                else if (round1Result == 0)
                    return new PlayerAction(Name, "Bet1", "check", 0);
                else
                    return new PlayerAction(Name, "Bet1", "bet", round1Result);
            }
        }

        //Actions for draw round
        public override PlayerAction Draw(Card[] hand)
        {
              //return var
            PlayerAction drawDecision;

            Evaluate.ListHand(Hand);

            //cards to discard
            List<int> discardCardsLocs = new List<int>();

            //player hand values
            Card highCard = null;
            int handVal = Evaluate.RateAHand(Hand, out highCard);
            Evaluate.SortHand(Hand);

            //If bluffing, get rid of up to two of our lowest cards so it looks like we still have a decent hand
            if(isBluffing)
            {
                discardCardsLocs = FindDiscardIndex();

                int numDiscarded = 0;

                foreach(int card in discardCardsLocs)
                {
                    if (Hand[card].Value <= 10)
                    {
                        ++numDiscarded;
                        Hand[card] = null;
                    }

                    if (numDiscarded >= 2)
                        break;
                }

                if (numDiscarded == 0)
                    drawDecision = new PlayerAction(Name, "Draw", "stand pat", 0);
                else
                    drawDecision = new PlayerAction(Name, "Draw", "draw", numDiscarded);

                return drawDecision;
            }

            //start from the top
            //straight and up = stand pat
            if (handVal >= 5)
            {
                drawDecision = new PlayerAction(Name, "draw", "stand pat", 0);
                return drawDecision;
            }

            //
            // use logan's straight/flush probabilities to determine if the risk is low enough
            // if(risk low)
            // {
            //      //discard necessary cards
            // }
            //

            //3 Of A Kind
            if (handVal == 4)
            {
                //find the two other cards location in hand
                discardCardsLocs = FindDiscardIndex();

                //set those to null
                foreach (int card in discardCardsLocs)
                {
                    Hand[card] = null;
                }

                //return
                drawDecision = new PlayerAction(Name, "draw", "draw", 2);
                return drawDecision;
            }

            //2 Pair
            if (handVal == 3)
            {
                //find the last card
                discardCardsLocs = FindDiscardIndex();

                //set it to null
                foreach (int card in discardCardsLocs)
                {
                    Hand[card] = null;
                }

                //return
                drawDecision = new PlayerAction(Name, "draw", "draw", 1);
                return drawDecision;
            }

            //1 Pair
            if (handVal == 2)
            {
                //consider flush/straight odds again, if odds meet a more lenient risk value go for it

                //else, find last 3
                discardCardsLocs = FindDiscardIndex();

                //set to null
                foreach (int card in discardCardsLocs)
                {
                    Hand[card] = null;
                }

                //return
                drawDecision = new PlayerAction(Name, "draw", "draw", 3);
                return drawDecision;
            }

            //HighCard
            else
            {
                //flush/straight odds  again, if odds meet a more lenient risk value go for it

                //check highcard value
                int highVal = highCard.Value;

                //if its high discard other 4
                if (highVal >= 10)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Hand[i] = null; //highcard is last in array
                    }

                    //return
                    drawDecision = new PlayerAction(Name, "draw", "draw", 4);
                    return drawDecision;
                }
                else //if its low discard all or bluff (discard 1)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        Hand[i] = null;
                    }

                    //return
                    drawDecision = new PlayerAction(Name, "draw", "draw", 5);
                    return drawDecision;
                }
            }

        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            PlayerAction lastOpponentAction = actions.Last();
            PlayerAction threeActsAgo = actions[actions.Count - 3];

            string lastActionName = lastOpponentAction.ActionName.ToLower();

            int round2Result;//Result that determines what action to take this round

            //If we haven't figured out how many cards the opponent discarded yet, this is our first time in round 2
            if (currentRound.NumCardsOpponentDiscarded < 0)
            {
                numTimesRaised = 0;

                //If last action was in draw phase, we know we're going first this round and we can get num discarded from that action
                if (lastOpponentAction.ActionPhase.ToLower().Equals("draw"))
                {
                    //Store number of cards discarded
                    if (lastActionName.Equals("draw"))
                        currentRound.StoreNumCardsDiscarded(lastOpponentAction.Amount);
                    else
                        currentRound.StoreNumCardsDiscarded(0);

                    //We're going first so get initial round 2 action
                    round2Result = GetRound2Action(-1);

                    //Return appropriate action based on result
                    if (round2Result == 0)
                        return new PlayerAction(Name, "Bet2", "check", 0);
                    else if(round2Result > 0)
                        return new PlayerAction(Name, "Bet2", "bet", round2Result);
                }
                //If we're going second, get discard info from action three items back
                else if (threeActsAgo.ActionPhase.ToLower().Equals("draw"))
                {
                    //If drew, store how many cards
                    if (threeActsAgo.ActionName.ToLower().Equals("draw"))
                        currentRound.StoreNumCardsDiscarded(threeActsAgo.Amount);
                    //Otherwise, they stood pat
                    else
                        currentRound.StoreNumCardsDiscarded(0);

                    //Get action to take
                    if (lastActionName.Equals("check"))
                    {
                        //Get action based on fact they checked
                        round2Result = GetRound2Action(0);

                        //If >0, bet
                        if (round2Result > 0)
                            return new PlayerAction(Name, "Bet2", "bet", round2Result);
                        //If 0, check
                        else if (round2Result == 0)
                            return new PlayerAction(Name, "Bet2", "check", 0);
                    }
                    else
                    {
                        //Get action based on how much they bet/raised by
                        round2Result = GetRound2Action(lastOpponentAction.Amount);

                        //If >0, raise
                        if (round2Result > 0)
                            return new PlayerAction(Name, "Bet2", "raise", round2Result);
                        //If 0, call
                        else if (round2Result == 0)
                            return new PlayerAction(Name, "Bet2", "call", 0);
                    } 
                }

                //If -1, fold
                return new PlayerAction(Name, "Bet2", "fold", 0);

            }
            //If we reach this point, the opponent has raised and we have to respond
            else
            {
                //Pass amount bet
                round2Result = GetRound2Action(lastOpponentAction.Amount);

                //If >0, raise
                if (round2Result > 0)
                    return new PlayerAction(Name, "Bet2", "raise", round2Result);
                //If 0, call
                else if (round2Result == 0)
                    return new PlayerAction(Name, "Bet2", "call", 0);
                //If -1, fold
                else
                    return new PlayerAction(Name, "Bet2", "fold", 0);

            }
        }

        //Calculate confidence in hand
        private float GetHandStrength()
        {
            Card highCard;

            //Clamp rating to value btwn 0-4
            //0 is high card, 4 is a straight or better - we treat 4+ hands all functionally the same since they're all such insanely rare/good hands
            int rating = Math.Min(Evaluate.RateAHand(Hand, out highCard) - 1, 4);

            float conf = handProbability[rating];

            //If we have a high card, modify confidence based on its value
            //Maximum confidence possible for a high card is 50%
            if(rating <= 1)
            {
                conf *= (float)(highCard.Value-1) / 13;
            }

            //If we have pairs, modify confidence by the strength of these pairs
            else if(rating >= 2 && rating <= 3)
            {
                //rating - 1 = number of pairs
                //Strength of pairs is value between 0-1, multiply it by the range between probability rating of this hand and the next worst hand
                //Essentially, a pair of 2s is only barely better than a high Ace, while a pair of Aces is almost as good as the worst three of a kind
                conf += ((float)GetValueOfHighestPair()/14) * (handProbability[rating] - handProbability[rating - 1]);
            }

            return conf;
        }

        //Use this to determine how strong our hand is if it's a one pair/two pair
        private int GetValueOfHighestPair()
        {
            int val = 0;

            //Check for pairs within cards and increase strength based on value of any pairs found
            //Since they're pre-sorted we know pairs will be right next to each other in a hand
            for(int i = 0; i < 4; ++i)
            {
                if(Hand[i].Value == Hand[i+1].Value)
                {
                    //Get the highest pair value because that's the one that counts
                    if(Hand[i].Value > val)
                    {
                        val = Hand[i++].Value;
                    }
                }
            }

            return val;//Return strength
        }
         private List<int> FindDiscardIndex()
        {
            //find cards w/o matches
            List<int> returnList = new List<int>();

            for (int i = 0; i < 5; i++)
            {
                //check value count for each card in hand
                int count = Evaluate.ValueCount(Hand[i].Value, Hand);

                //no match
                if (count <= 1)
                {
                    returnList.Add(i); //index of card
                }
            }
            return returnList;
        }

        //Look at the ratio for how much you'll have to pay vs the potential payout if you win
        //Lower pot odds are less worth the risk - balance this with confidence in hand
        private float GetPotOdds()
        {
            float amtNeeded = currentRound.OpponentBetAmt - currentRound.PlayerBetAmt;

            float potOdds = amtNeeded / (float)currentRound.Pot;

            //Divide total pot by amount you need to spend to call
            //Invert percentage so that higher % is better
            return 1f - potOdds;
        }

        ///Returns a key value pair of the longest consecutive set of cards by value (position start, amount consecutive)
        private KeyValuePair<int, int> HighestConsecutiveSet()
        {
            int consecutiveCount = 1;

            //final position, amount consecutive
            Dictionary<int, int> consecutiveSets = new Dictionary<int, int>();

            //Determine consecutive cards and times there are sets
            for(int i = 1; i <= 4; i++)
            {
                if(Hand[i-1].Value == Hand[i].Value - 1)
                {
                    consecutiveCount++;
                }
                else if(consecutiveCount == 1)
                    continue;
                else
                {
                    consecutiveSets.Add(i, consecutiveCount);
                    consecutiveCount = 1;
                }
            }

            int highestConsecutive = 1;
            int highestConsecPos = 0;

            //Find the greatest set in the hand
            foreach(int key in consecutiveSets.Keys)
            {
                if(consecutiveSets[key] > highestConsecutive)
                {
                    highestConsecutive = consecutiveSets[key];
                    highestConsecPos = key - (highestConsecutive - 1);
                }
            }

            //Returns key value pair of the starting position and consecutive number of cards by value
            return new KeyValuePair<int, int>(highestConsecPos, highestConsecutive);
        }

        //Returns a dictionary of the amount of cards in each suite within the hand
        private Dictionary<string, int> CardsPerSuite()
        {
            //Counts for each suite type
            int heartCount = 0;
            int spadeCount = 0;
            int diamondCount = 0;
            int clubsCount = 0;

            Dictionary<string, int> cardsPerSuite = new Dictionary<string, int>();

            for(int i = 0; i < Hand.Length; i++)
            {
                if(Hand[i].Suit == "Hearts")
                    heartCount++;
                else if(Hand[i].Suit == "Diamonds")
                    diamondCount++;
                else if(Hand[i].Suit == "Spades")
                    spadeCount++;
                else if(Hand[i].Suit == "Clubs")
                    clubsCount++;
            }

            cardsPerSuite.Add("Hearts", heartCount);
            cardsPerSuite.Add("Diamonds", diamondCount);
            cardsPerSuite.Add("Spades", spadeCount);
            cardsPerSuite.Add("Clubs", clubsCount);

            return cardsPerSuite;
        }

        //Returns random amount to bet/raise by, or 0 to call/check if we can't afford it
        private int GetBetAmount(int min, int max)
        {
            int amt = rand.Next(min, max + 1);

            //Make sure not getting into a raising war - max of 2 raises per betting round before we check
            //If bet amt would leave us unable to afford ante next round, just check and pray
            if (numTimesRaised < 2 && amt < Money - currentRound.Ante)
            {
                ++numTimesRaised;
                return amt;
            }
            else
            {
                return 0;
            }
        }

        //Return -1 to fold
        //Return >0 if bluffing to bet/raise
        private int BluffOrFold(float handStrength)
        {
            //Randomly decide whether to bluff, chances of doing it will increase over time
            if (rand.Next(1, 11) < bluffChance)
            {
                isBluffing = true;//Start bluffing the rest of this game
                bluffChance = 0;//Reset chance of bluffing next time
                
                //Decide how much to bet, modify by hand's strength so we don't risk too much
                return (int)(GetBetAmount(15, 25) * handStrength);
            }
            //Fold if we're not going to bluff with this bad hand
            else
            {
                ++bluffChance;//Increase chances of bluffing in future
                return -1;
            }
        }

        //Return -1 to fold
        //Return 0 to check/call
        //Return >0 to bet/raise
        private int GetRound1Action(int opponentAction)//Pass -1 if we're going first, 0 if opponent checked, >0 for how much opponent bet/raised by
        {
            //Rate our hand
            Card highCard;
            int handRating = Evaluate.RateAHand(Hand, out highCard);

            //Strength based on likelihood of hand occurring
            float handStrength = GetHandStrength();

            //Get pot odds - ratio of how much you'd have to pay to stay in vs potential payout
            //Low pot odds and low hand strength -> fold/bluff
            //Low pot odds and high hand strength -> call/check
            //High pot odds and low hand strength -> call/check
            //High pot odds and high hand strength -> bet/raise
            float potOdds = GetPotOdds();

            //If opponent bet/raised
            if (opponentAction > 0)
            {
                //Bad hand
                if (handRating <= 2)
                {
                    //Bad pot odds - fold unless we're bluffing
                    if (potOdds < handStrength)
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is bad and pot odds of " + potOdds + " are bad; will either bluff or fold ---\n");
                        return BluffOrFold(handStrength);
                    }
                    //Good pot odds - call even though hand is bad b/c bet is low risk
                    else
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is bad but pot odds of " + potOdds + " are decent; will call ---\n");
                        return 0;
                    }
                }
                //Decent hand - two pair or three of a kind
                else if (handRating <= 4)
                {
                    //If we have to pay over 75% of the pot's current value to stay in, the pot odds are bad
                    if (potOdds < 0.25f)
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is decent but pot odds of " + potOdds + " are bad; will either bluff or fold ---\n");
                        //Consider bluffing but our hand probably isn't good enough so most likely fold
                        return BluffOrFold(handStrength);
                    }
                    //Pretty favorable pot odds, let's raise!
                    else if (potOdds > 0.75f)
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is decent and pot odds of " + potOdds + " are great; will raise ---\n");
                        //Bet slightly more conservatively though
                        return GetBetAmount(5, 15);
                    }
                    //Pot odds aren't awesome but why not give it a shot?
                    else
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is decent and pot odds of " + potOdds + " are okay; will call ---\n");
                        return 0;
                    }
                }
                //We've got a great hand!
                else
                {
                    //If we have fairly favorable pot odds, keep things going
                    if (potOdds > 0.5f)
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is good and pot odds of " + potOdds + " are fairly favorable; will raise ---\n");
                        return GetBetAmount(15, 25);
                    }
                    //If we have bad pot odds, this hand is still good, just call to stay in
                    else
                    {
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is good but pot odds of " + potOdds + " aren't great; will call ---\n");
                        return 0;
                    }
                }
            }
            //If opponent checked, that's probably a good sign for us
            else
            {
                //If our hand is fairly weak, check too unless we decide to bluff and spook them
                if (handRating <= 2)
                {
                    Console.WriteLine("\n--- Hand rating of " + handRating + " isn't great; will either bluff or check ---\n");

                    //Consider bluffing, but if not then check rather than folding
                    return Math.Max(BluffOrFold(handStrength), 0);
                }
                //Otherwise our hand is good! Let's bet!
                else if(opponentAction == 0 || handRating >= 5)
                {
                    Console.WriteLine("\n--- Hand rating of " + handRating + " is good, especially since they checked; will bet aggressively ---\n");
                    return GetBetAmount(20, 30);
                }
                //Otherwise we're going first and our hand isn't incredible, just bet conservatively
                else
                {
                    Console.WriteLine("\n--- Hand rating of " + handRating + " is fairly good; will bet conservatively ---\n");
                    return GetBetAmount(10, 20);
                }
            }
        }

        //Return -1 to fold
        //Return 0 to check/call
        //Return >0 to bet/raise
        private int GetRound2Action(int opponentAction)//Pass -1 if we're going first, 0 if opponent checked, >0 for how much opponent bet/raised by
        {
            //Rate our hand
            Card highCard;
            int handRating = Evaluate.RateAHand(Hand, out highCard);

            //Strength based on likelihood of hand occurring
            float handStrength = GetHandStrength();

            //Get pot odds - ratio of how much you'd have to pay to stay in vs potential payout
            //Low pot odds and low hand strength -> fold/bluff
            //Low pot odds and high hand strength -> call/check
            //High pot odds and low hand strength -> call/check
            //High pot odds and high hand strength -> bet/raise
            float potOdds = GetPotOdds();

            //If they checked and we also have bad hand, check as well
            if (opponentAction == 0)
            {
                //Bad hand and not bluffing - just check and see how things go
                if (handRating <= 2 && !isBluffing)
                {
                    Console.WriteLine("\n--- Hand rating of " + handRating + " is bad but theirs probably is too; will check ---\n");
                    return 0;
                }
                //Decent hand and they checked? Let's apply pressure
                else
                {
                    if (isBluffing)
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is bad but we're bluffing - will apply some pressure for them to fold; will bet ---\n");
                    else
                        Console.WriteLine("\n--- Hand rating of " + handRating + " is good and they checked - will apply some pressure for them to fold; will bet ---\n");

                    return GetBetAmount(15, 25);
                }
            }

            //If responding to bet/raise, determine what to do based on how many cards opponent discarded in draw phase
            switch(currentRound.NumCardsOpponentDiscarded)
            {
                //They stood pat... Likely have a strong hand but could be bluffing
                case 0:
                    Console.WriteLine("\n--- They stood pat; likely have a straight or better unless bluffing ---");

                    //If we have a straight or better, let's go for it!
                    //Bet/raise, limit raising back and forth to two times before calling to end the round
                    if (handRating >= 5)
                    {
                        //If hand or pot odds are favorable, bet aggressively
                        if (handRating >= 7 || potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is great or pot odds of " + potOdds + " are great; will raise aggressively ---\n");
                            return GetBetAmount(15, 30);
                        }
                        //If pot odds are less favorable, bet slightly more conservatively
                        else if(potOdds > 0.5f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is good but pot odds of " + potOdds + " aren't amazing; will raise conservatively ---\n");
                            return GetBetAmount(10, 20);
                        }
                        //Never fold in this scenario
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is good but pot odds of " + potOdds + " aren't amazing; will call ---\n");
                            return 0;
                        }
                    }
                    //If our hand is a three of a kind, only stick with it if we've got really good pot odds
                    else if(handRating == 4 && potOdds > 0.7f)
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " isn't ideal but pot odds are favorable; will call ---\n");
                        return 0;
                    }
                    //If hand isn't good enough to be confident against a hand that justifies standing pat, nope out of there and fold
                    else
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " isn't good enough to justify staying in; will fold ---\n");
                        return -1;
                    }
                    break;
                //They discarded one... Likely indicates a 2 pair
                case 1:
                    Console.WriteLine("\n--- They discarded 1 card; likely have a 2 pair ---");
                    //If hand is a straight or better, we should be good - bet/raise
                    if (handRating >= 5)
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is great; will raise ---\n");
                        //Bet relatively aggressively
                        return GetBetAmount(15,25);
                    }
                    //Three of a kind, probably better than them but tread lightly
                    else if(handRating == 4)
                    {
                        //If pot odds are favorable, raise
                        if(potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is likely good enough and pot odds are favorable; will raise ---\n");
                            //Bet conservatively
                            return GetBetAmount(5,15);
                        }
                        //Otherwise, call
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is likely good enough; will call ---\n");
                            return 0;
                        }
                    }
                    //If we have a 2 pair, only stick with it if we have a strong high pair
                    else if(handRating == 3)
                    {
                        //If we have a strong 2 pair (Aces/Kings) or favorable pot odds, call
                        if(GetValueOfHighestPair() > 12 || potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " isn't ideal but pairs are strong or pot odds of " + potOdds + " are favorable ---\n");
                            return 0;
                        }
                        //Otherwise, fold
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " isn't good enough to justify staying in; will fold ---\n");
                            return -1;
                        }
                    }
                    //If worse than a 2 pair, we don't have a chance - fold
                    else
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " isn't good enough to justify staying in; will fold ---\n");
                        return -1;
                    }
                    break;
                //They discarded two... Likely indicates a 3 pair
                case 2:
                    Console.WriteLine("\n--- They discarded 2 cards; likely have a 3 pair ---");
                    //If hand is a straight or better, we're in a good place - bet/raise
                    if (handRating >= 5)
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is great; will raise ---\n");
                        return GetBetAmount(15, 25);
                    }
                    //If hand is a 3 pair, call or raise if pot odds are good
                    else if(handRating == 4)
                    {
                        //If pot odds are favorable, raise
                        if (potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is likely good enough and pot odds are favorable; will raise ---\n");
                            //Bet conservatively
                            return GetBetAmount(5, 15);
                        }
                        //Otherwise, call
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is likely good enough; will call ---\n");
                            return 0;
                        }
                    }
                    //If hand is worse than a 3 pair, pack it up and go home
                    else
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is likely not good enough; will fold ---\n");
                        return -1;
                    }
                    break;
                //They discarded three... Likely indicates the best they have is a one pair
                case 3:
                    Console.WriteLine("\n--- They discarded 3 cards; likely have a 1 pair ---");
                    //If hand is a straight or better, bet/raise
                    if (handRating >= 3)
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is great; will raise ---\n");
                        //Bet aggressively
                        return GetBetAmount(10, 20);
                    }
                    //If hand is a one pair and opponent is trying to raise, fold if our pair is low
                    else if(handRating == 2)
                    {
                        //If our pair is Kings/Aces, call
                        if(GetValueOfHighestPair() > 12 || potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is okay; will call ---\n");
                            return 0;
                        }
                        //Otherwise I don't like our odds, let's fold
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " and pot odds of " + potOdds + " aren't good enough; will fold ---\n");
                            return -1;
                        }
                    }
                    //If only have high card, fold
                    else
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is bad; will fold ---\n");
                        return -1;
                    }
                    break;
                //They discarded 4/5... Their hand is probably a trainwreck
                default:
                    Console.WriteLine("\n--- They discarded 4/5 cards; likely have a bad hand ---");
                    //If hand is a two pair or better, bet/raise
                    //This is also a pretty safe environment for bluffing so why not give it a go!
                    if (handRating >= 3 || isBluffing)
                    {
                        Console.WriteLine("--- Hand rating of " + handRating + " is good or currently bluffing; will call ---\n");
                        return GetBetAmount(15, 25);
                    }
                    //If hand is only a one pair, call if pair is low
                    else if(handRating == 2)
                    {
                        //If have a high one pair or good pot odds, raise
                        if(GetValueOfHighestPair() > 10 || potOdds > 0.75f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is okay or pot odds of " + potOdds + " are good; will raise ---\n");
                            return GetBetAmount(5,15);
                        }
                        //Call if we have a bad pair and bad pot odds
                        else if(potOdds > 0.5f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is okay and pot odds of " + potOdds + " are okay; will call ---\n");
                            return 0;
                        }
                        //Fold if pot odds are bad too
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is bad and pot odds of " + potOdds + " is bad; will fold ---\n");
                            return -1;
                        }
                    }
                    //If hand is only a high card, fold unless odds are good
                    else
                    {
                        //Call if have a high ace or favorable pot odds
                        if(highCard.Value == 14 || potOdds > 0.8f)
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is bad but have an Ace or pot odds of " + potOdds + " is good; will call ---\n");
                            return 0;
                        }
                        //Otherwise, fold
                        else
                        {
                            Console.WriteLine("--- Hand rating of " + handRating + " is bad and pot odds of " + potOdds + " is bad; will fold ---\n");
                            return -1;
                        }
                    }
                    //Otherwise just stay in and call/check
                    break;
            }

        }

    }

    class Round
    {
        private int roundNum;//Keeps track of current round number

        private int ante;//Ante of current round, increases as game goes on
        private int pot;//Pot for current roud, increases as players bet
        private int opponentBetAmt;//How much the opponent has bet this round so far
        private int playerBetAmt;//How much we have bet this round so far

        private int numCardsOpponentDiscarded;//Number of cards opponent discarded

        //Props
        public int Ante { get { return ante; } }
        public int Pot { get { return pot; } }
        public int OpponentBetAmt { get { return opponentBetAmt; } }
        public int PlayerBetAmt { get { return playerBetAmt; } }
        public int NumCardsOpponentDiscarded { get { return numCardsOpponentDiscarded; } }

        //Constructor takes current round number
        public Round()
        {
            roundNum = 0;
            AdvanceRound();
        }

        public void AdvanceRound()
        {
            ++roundNum;//Increment round num

            //Calculate ante based on current round
            if (roundNum > 50)
            {
                ante = 30;
            }
            else if (roundNum > 25)
            {
                ante = 20;
            }
            else
            {
                ante = 10;
            }

            //Reset vars
            pot = ante * 2;

            opponentBetAmt = 0;
            playerBetAmt = 0;
            numCardsOpponentDiscarded = -1;
        }

        public void AddPlayerBet(int amt)
        {
            playerBetAmt = opponentBetAmt + amt;
            pot = ante + playerBetAmt + opponentBetAmt;
        }

        public void AddOpponentBet(int amt)
        {
            opponentBetAmt = playerBetAmt + amt;
            pot = ante + playerBetAmt + opponentBetAmt;
        }

        public void StoreNumCardsDiscarded(int amt)
        {
            numCardsOpponentDiscarded = amt;
        }
    }
}

