using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    class Player4 : Player
    {
        Random rand;//Random number generator to determine what actions are taken based on confidence probability
        float confidence;//Number between 0-1 that represents confidence that have winning hand

        int currentRoundNumber;//Number of the round we're currently on
        int opponentMoney;//Keeps track of how much money the opponent has

        int numTimesRaised;//Keep track of number of times we've raised

        bool isBluffing;

        Round currentRound;//Object keeps track of important info about this round
        
         //bet2 vars
        int oppHandVal, oppDiscards, actionCount;
        bool firstTurn = true; //reset every round somehow (in bet1?)

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
            //confidence = GetHandStrength();//Get confidence for current hand

            //If they're going first, get their action so we can factor that in
            //Can call, raise, or fold
            if (actions.Count > 0)
            {
                if(actions.Count <= 1)
                {
                    NewRound();
                }

                PlayerAction opponentAction = actions.Last();

                int oppBetAmt = 0;//How much opponent just bet

                //If the opponent bet/raised, determine whether to call, raise, or fold
                //Calling will end the betting round
                //Raising will send it back around to the opponent
                //Folding will obviously mean we lose
                if (opponentAction.ActionName == "bet" || opponentAction.ActionName == "raise")
                {
                    oppBetAmt = opponentAction.Amount;
                    opponentMoney -= oppBetAmt;
                    currentRound.AddOpponentBet(oppBetAmt);

                    //How much have they bet? Would the amount of money we'd have to pay to match them be worth the risk?
                    float potOdds = GetPotOdds(oppBetAmt);

                    //If we don't have enough money to match, fold
                    //Otherwise, if the pot odds are bad and our confidence is low, randomly decide whether we're going to fold based on confidence
                    //Lower confidence = more likely to fold
                    if (oppBetAmt > Money || (potOdds - confidence > 0.5f && rand.NextDouble() > confidence))
                    {
                        return new PlayerAction(Name, "Bet1", "fold", 0);
                    }
                    //We're staying in! Random chance of only calling based on confidence
                    else if (rand.NextDouble() > confidence)
                    {
                        return new PlayerAction(Name, "Bet1", "call", 0);
                    }
                    //If not folding or calling, raise
                    else
                    {
                        //Get amount to raise by as random number between 0 and absolute max of 25
                        //Varies based on confidence rating
                        int betAmt = GetAmountToBet();

                        return new PlayerAction(Name, "Bet1", "raise", betAmt);
                    }
                }
                //The only other option is that they're checking
                else//opponentAction.ActionName == "check"
                {
                    //Wow, they checked right off the bat?
                    //Increase confidence by 10% because they probably have a bad hand
                    confidence = Math.Min(1f, confidence * 1.1f);

                    //Definitely don't fold! Decide whether to check as well or raise
                    if (rand.NextDouble() > confidence)
                    {
                        return new PlayerAction(Name, "Bet1", "check", 0);
                    }
                    //If not folding or calling, raise
                    else
                    {
                        //Get amount to raise by as random number between 0 and absolute max of 25
                        //Varies based on confidence rating
                        int betAmt = GetAmountToBet();

                        return new PlayerAction(Name, "Bet1", "bet", betAmt);
                    }
                }
            }
            //If we're going first, will have to base decision entirely off of hand strength
            //Can check, bet, or fold
            else
            {
                NewRound();//Start new round

                //Determine what to do, we're going first
                if (rand.NextDouble() > confidence)
                {
                    return new PlayerAction(Name, "Bet1", "check", 0);
                }
                //If not folding or checking, bet
                //Get amount to bet as random number between 0 and absolute max of 25
                //Varies based on confidence rating
                else
                {
                    int betAmt = GetAmountToBet();

                    return new PlayerAction(Name, "Bet1", "bet", betAmt);
                }      
            }
        }

        //Actions for draw round
        public override PlayerAction Draw(Card[] hand)
        {
                       //Discard List
            List<int> discardList = new List<int>();

            //Check if lower than a straight
            if(hand.Rank <= 4)
            {
                //Evaluate straights or flushes in the hand
                KeyValuePair<int, int> consecutiveSet = HighestConsecutiveSet();

                Dictionary<string, int> cardsPerSuite = CardsPerSuite();

                //Determine probabilities of finishing a straight
                int amountForStraight = 5 - consecutiveSet.Value;
                float straightChance = 4.0f / (amountForStraight * 42.0f);

                //Determine probabilities of finishing a flush
                float flushChance = 0.0f; 

                foreach(KeyValuePair<string, int> pair in cardsPerSuite)
                {
                    //Reasonable chance to get a flush
                    if(cardsPerSuite[pair.Key] >= 3)
                    {
                        if(cardsPerSuite[pair.Key] == 3)
                            flushChance = 0.125f;  //1 in 8 chance : does not account for less cards in deck
                        else
                            flushChance = 0.25f;  //1 in 4 chance : does not account for less cards in deck
                    }
                }
                //2 or less cards of same suite, too low of a chance to consider
                if(flushChance == 1.0f)
                    flushChance = 0.007f;

                //Discard logic here

                

                //Set up discard list
                switch(hand.Rank)
                {
                    //High card
                    case 1:

                        //Should it try for a flush or try for a straight?
                        if(flushChance > 0.125f)
                        {
                            foreach(KeyValuePair<string, int> pair in cardsPerSuite)
                            {
                                if(cardsPerSuite[pair.Key] >= 3)
                                {
                                    for(int i = 0; i < 5; i++)
                                    {
                                        if(hand[i].Suit != pair.Key)
                                            discardList.Add(i);
                                    }
                                }
                            }
                        }
                        else if(straightChance >= 0.09)
                        {
                            //Discard the card not consecutive
                            for(int i = 0; i < hand.Count(); i++)
                            {
                                int count = Evaluate.ValueCount(i, Hand);
                                if(count == 1)
                                {
                                    discardList.Add(i);
                                    break;
                                }
                            }
                        }
                        else
                        {
                            //Discard
                            for(int i = 0; i < 5; i++)
                            {
                                discardList.Add(i);
                            }
                        }
                        //Bluff?

                        break;
                    //One Pair
                    case 2:

                        //Should it try for a flush or try for a straight?


                        //Bluff?

                        //Discard the cards not in the pair
                        for(int i = 0; i < hand.Count(); i++)
                        {
                            int count = Evaluate.ValueCount(i, Hand);
                            if(count == 1)
                                discardList.Add(i);
                        }
                        
                        break;
                    //Two Pair
                    case 3:
                        //Discard the card not in a pair
                        for(int i = 0; i < hand.Count(); i++)
                        {
                            int count = Evaluate.ValueCount(i, Hand);
                            if(count == 1)
                                discardList.Add(i);
                        }
                        break;
                    //3 of a kind
                    case 4:
                        //Discard
                        if(consecutiveSet.Key != 0)
                        {
                            //High card is in 3 of a kind
                            if(consecutiveSet.Key + 3 == 4)
                            {
                                discardList.Add(0);
                                discardList.Add(1);
                            }
                            else
                            {
                                discardList.Add(0);
                                discardList.Add(4);
                            }
                        }
                        //Low 3 of a kind
                        else if(consecutiveSet.Key == 0)
                        {
                            discardList.Add(3);
                            discardList.Add(4);
                        }
                        break;
                }

                //Discard any cards
                for(int i = 0; i < discardList.Count(); i++)
                {
                    hand[discardList[i]] = null;
                }
                        
            }

            return new PlayerAction(Name, "Draw", "draw", discardList.Count());
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            PlayerAction lastOpponentAction = actions.Last();
            PlayerAction threeActsAgo = actions[actions.Count - 3];

            int round2Result;

            //If we haven't figured out how many cards the opponent discarded yet, this is our first time in round 2
            if(currentRound.NumCardsOpponentDiscarded < 0)
            {
                //If last action was in draw phase, we know we're going first this round and we can get num discarded from that action
                if(lastOpponentAction.ActionPhase.ToLower().Equals("draw"))
                {
                    //Store number of cards discarded
                    if (lastOpponentAction.ActionName.ToLower().Equals("draw"))
                        currentRound.StoreNumCardsDiscarded(lastOpponentAction.Amount);
                    else
                        currentRound.StoreNumCardsDiscarded(0);

                    //We're going first so get initial round 2 action
                    round2Result = GetRound2Action(-1);

                    //Return appropriate action based on result
                    if (round2Result == 0)
                        return new PlayerAction(Name, "Bet2", "call", 0);
                    else if (round2Result < 0)
                        return new PlayerAction(Name, "Bet2", "fold", 0);
                    else
                        return new PlayerAction(Name, "Bet2", "raise", round2Result);
                }
                //If we're going second, get discard info from action three items back
                else if (threeActsAgo.ActionPhase.ToLower().Equals("draw"))
                {
                    if (threeActsAgo.ActionName.ToLower().Equals("draw"))
                        currentRound.StoreNumCardsDiscarded(threeActsAgo.Amount);
                    else
                        currentRound.StoreNumCardsDiscarded(0);
                }                
            }

            //Get the action we must do this turn
            if (lastOpponentAction.ActionName.ToLower().Equals("raise") || lastOpponentAction.ActionName.ToLower().Equals("bet"))
            {
                currentRound.AddOpponentBet(lastOpponentAction.Amount);
                //Pass amount bet
                round2Result = GetRound2Action(lastOpponentAction.Amount);
            }
            else
                //Pass 0 because they checked
                round2Result = GetRound2Action(0);

            //If 0, call
            if (round2Result == 0)
                return new PlayerAction(Name, "Bet2", "call", 0);
            //If -1, fold
            else if (round2Result < 0)
                return new PlayerAction(Name, "Bet2", "fold", 0);
            //If >0, raise by that amount
            else
                return new PlayerAction(Name, "Bet2", "raise", round2Result);

            /*
            if (Dealer)
            {
                

                int oppBetAmt = 0;//How much opponent just bet

                //If the opponent bet/raised, determine whether to call, raise, or fold
                //Calling will end the betting round
                //Raising will send it back around to the opponent
                //Folding will obviously mean we lose
                if (lastOpponentAction.ActionName == "bet" || lastOpponentAction.ActionName == "raise")
                {
                    oppBetAmt = lastOpponentAction.Amount;
                    opponentMoney -= oppBetAmt;
                    currentRound.AddOpponentBet(oppBetAmt);

                    
                }
                //The only other option is that they're checking
                else//opponentAction.ActionName == "check"
                {
                    //Definitely don't fold! Decide whether to check as well or raise
                    if (rand.NextDouble() > confidence)
                    {
                        return new PlayerAction(Name, "Bet2", "check", 0);
                    }
                    //If not folding or calling, raise
                    else
                    {
                        //Get amount to raise by as random number between 0 and absolute max of 25
                        //Varies based on confidence rating
                        int betAmt = GetAmountToBet();

                        return new PlayerAction(Name, "Bet2", "bet", betAmt);
                    }
                }
            }
            //If we're going first, will have to base decision entirely off of hand strength
            //Can check, bet, or fold
            else
            {
                //Result on what to do for this betting round
                //-1 = fold
                //0 = check/call
                //>0 = raise by that amount
                //int round2Result;

                //If last action was in draw phase, we're going first here
                if (lastOpponentAction.ActionPhase.Equals("Draw"))
                {
                    if (lastOpponentAction.ActionName.Equals("draw"))
                        currentRound.StoreNumCardsDiscarded(lastOpponentAction.Amount);
                    else
                        currentRound.StoreNumCardsDiscarded(0);

                    round2Result = GetRound2Action(-1);

                    if (round2Result == 0)
                        return new PlayerAction(Name, "Bet2", "check", 0);
                    else if (round2Result < 0)
                        return new PlayerAction(Name, "Bet2", "fold", 0);
                    else
                        return new PlayerAction(Name, "Bet2", "bet", round2Result);
                }
                else
                {
                    if (lastOpponentAction.ActionName.Equals("check"))
                        round2Result = GetRound2Action(0);
                    else
                        round2Result = GetRound2Action(lastOpponentAction.Amount);

                    
                }
                
                //Determine what to do, we're going first
                if (rand.NextDouble() > confidence)
                {
                    return new PlayerAction(Name, "Bet2", "check", 0);
                }
                //If not folding or checking, bet
                //Get amount to bet as random number between 0 and absolute max of 25
                //Varies based on confidence rating
                else
                {
                    int betAmt = GetAmountToBet();

                    return new PlayerAction(Name, "Bet2", "bet", betAmt);
                }
            }*/
        }
        
         private int GetOpponentDiscards(List<PlayerAction> actions)
        {
            if(actions[actionCount].ActionPhase == "draw" && actions[actionCount].Name != Name)//draw phase + not your turn (this applies if you drew first)
            {
                //this is the opponents last drawing phase
                return actions[actionCount].Amount;
            }
            else //they drew first
            {
                return actions[actionCount - 2].Amount;
            }
        }

        private void EvaluateOpponentsHand() //numbers should be scaled to match bet1 confidence scale
        {
            switch(oppDiscards)
            {
                case 0: //opponent did not discard (probably a very good hand)
                    oppHandVal = 10;
                    break;
                case 1: //opponent has either 2 pair or 4 of a kind
                    oppHandVal = 8; //or 3?
                    break;
                case 2: //opponent has 3 of a kind
                    oppHandVal = 4;
                    break;
                case 3: //opponent has a pair
                    oppHandVal = 2;
                    break;
                case 4: //opponent discarded all but their high card (so it might be decently high)
                    oppHandVal = 1;
                    break;
                default: //errors or all discarded
                    oppHandVal = 0;
                    break;
            }

            firstTurn = false;
        }

        //Randomly determine what to bet based on confidence
        private int GetAmountToBet()
        {
            //Absolute max is 25 when at max confidence
            int betAmt = rand.Next(1, (int)(25 * confidence));

            betAmt = Math.Min(betAmt, Money - currentRound.OpponentBetAmt-currentRound.PlayerBetAmt);

            return betAmt;
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

                if (rand.NextDouble()/2 > conf)
                    isBluffing = true;
            }

            //If we have pairs, modify confidence by the strength of these pairs
            else if(rating >= 2 && rating <= 3)
            {
                //rating - 1 = number of pairs
                //Strength of pairs is value between 0-1, multiply it by the range between probability rating of this hand and the next worst hand
                //Sssentially, a pair of 2s is only barely better than a high Ace, while a pair of Aces is almost as good as the worst three of a kind
                conf += ((float)GetValueOfHighestPair()/13) * (handProbability[rating] - handProbability[rating - 1]);
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

        //Look at the ratio for how much you'll have to pay vs the potential payout if you win
        //Lower pot odds are less worth the risk - balance this with confidence in hand
        private float GetPotOdds(int opponentBetAmt)
        {
            //Divide total pot by amount you need to spend to call
            //Invert percentage so that higher % is better
            return 1 - ((float)opponentBetAmt / currentRound.Pot);
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

        //Return 0 to check/call
        //Return >0 to bet/raise
        private int GetRound1Action(int opponentAction)
        {
            //Rate our hand
            Card highCard;
            int handRating = Evaluate.RateAHand(Hand, out highCard);

            //If they checked and we also have a bad hand, there'll be small chance to bluff but more often just check as well
            if(opponentAction == 0 && handRating < 2)
            {
                if(rand.NextDouble() > GetHandStrength())
                {

                }
            }

            return 0;
        }

        //Return -1 to fold
        //Return 0 to check/call
        //Return >0 to bet/raise
        private int GetRound2Action(int opponentAction)//Pass -1 if we're going first, 0 if opponent checked, >0 for how much opponent bet/raised by
        {
            //Rate our hand
            Card highCard;
            int handRating = Evaluate.RateAHand(Hand, out highCard);

            //If they checked and we also have bad hand, check as well
            if(opponentAction == 0 && handRating < 2)
            {
                //If bluffing, randomly bet based on high card value
                if (isBluffing)
                {
                    //Pick random amount based on high card val
                    int amt = rand.Next(highCard.Value, highCard.Value * 2);

                    //If bet amt would leave us unable to afford ante next round, just check and pray
                    if (amt < Money - currentRound.Ante)
                        return amt;                    
                }

                //Default to checking - folding will just lose us money at this point
                return 0;              
            }
            //If opponent is trying to raise and we've already done it twice, that's enough, just call to end
            else if(opponentAction > 0 && numTimesRaised >= 1)
            {
                return 0;
            }

            //Determine what to do based on how many cards opponent discarded in draw phase
            switch(currentRound.NumCardsOpponentDiscarded)
            {
                //They stood pat... Likely have a strong hand but could be bluffing
                case 0:
                    //If we have a full house or better, let's go for it!
                    //Bet/raise, limit raising back and forth to two times before calling to end the round
                    if (handRating >= 7)
                    {
                        ++numTimesRaised;
                        return rand.Next(10,21);//Bet between 10 and 20
                    }
                    //If hand isn't good enough to be confident against a hand that justifies standing pat
                    //and opponent is raising, nope out of there and fold
                    else if(handRating <= 4 && opponentAction > 0)
                    {
                        return -1;
                    }
                    //Otherwise, if our hand is a straight or better or opponent called/checked, just call/check to stay in and see where it goes
                    else
                    {
                        return 0;
                    }
                    break;
                //They discarded one... Likely indicates a 2 pair
                case 1:
                    //If hand is a straight or better, we should be good - bet/raise
                    if(handRating >= 5)
                    {
                        ++numTimesRaised;
                        return rand.Next(10,21);//Bet between 10 and 20
                    }
                    
                    //If opponent is raising and we have a weaker hand, fold unless we have a strong 2 pair
                    else if (handRating <= 3 && opponentAction > 0)
                    {
                        //If we have a 2 pair, evaluate how strong it is - if our high pair is King/Ace, let's give it a shot
                        if(handRating == 3 && GetValueOfHighestPair() > 12)
                        {
                            return 0;
                        }

                        //If hand is worse than a 2 pair or a bad 2 pair, we're donezo - fold
                        return -1;
                    }
                    //Otherwise, we probably have a shot - call/check to stay in
                    break;
                //They discarded two... Likely indicates a 3 pair
                case 2:
                    //If hand is a straight or better, we're in a good place - bet/raise
                    if (handRating >= 5)
                    {
                        ++numTimesRaised;
                        return rand.Next(10, 21);//Bet between 10-20
                    }
                    //If hand is worse than a 3 pair and opponent wants to raise, pack it up and go home
                    else if (handRating <= 3 && opponentAction > 0)
                    {
                        return -1;
                    }
                    //Otherwise, it's worth a try - call/check to stay in
                    break;
                //They discarded three... Likely indicates the best they have is a one pair
                case 3:
                    //If hand is a straight or better, bet/raise
                    if(handRating >= 4)
                    {
                        ++numTimesRaised;
                        return rand.Next(15, 26);//Bet between 15-25
                    }
                    //If hand is a one pair and opponent is trying to raise, fold if our pair is low
                    else if(handRating <= 2 && opponentAction > 0)
                    {
                        //If our pair is Kings/Aces, call
                        if(handRating == 2 && GetValueOfHighestPair() > 12)
                        {
                            return 0;
                        }
                        //Otherwise I don't like our odds, let's fold
                        else
                        {
                            return -1;
                        }
                    }
                    //Otherwise, we'll stay in and call/check
                    break;
                //They discarded 4/5... Their hand is probably a trainwreck
                default:
                    //If hand is a two pair or better, bet/raise
                    //This is also a pretty safe environment for bluffing so why not give it a go!
                    if(handRating >= 3 || isBluffing)
                    {
                        ++numTimesRaised;
                        return rand.Next(15,26);
                    }
                    //If hand is only a one pair, fold if pair is low
                    else if(handRating <= 2 && opponentAction > 0)
                    {
                        //Call/check if pair is King or better
                        if(handRating == 2 && GetValueOfHighestPair() > 12)
                        {
                            return 0;
                        }
                        //Fold if we have a bad pair
                        else 
                        {
                            return -1;
                        }
                    }
                    //Otherwise just stay in and call/check
                    break;
            }

            //Call/check by default
            return 0;
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

