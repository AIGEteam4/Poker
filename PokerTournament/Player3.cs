using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerTournament
{
    // AI player implementation from Hatin, Parker, Probeck
    class Player3 : Player
    {
        // enum to determine liklihood of getting a particular hand
        enum PossibilityLevel { Have, Likely, Possible, Unlikely, Poor, Impossible }; // based on amount of cards to discard. if have a hand, discard 0

        List<RefractionCard> refHand;
        List<int> rankOfTargetHands;
        int targetHandRank;
        Random rnd;

        // construct object with super class variables
        public Player3(int playerId, string playerName, int totalMoney) : base(playerId, playerName, totalMoney)
        {
            refHand = new List<RefractionCard>();
            rankOfTargetHands = new List<int>();
            rnd = new Random();
        }

        public override PlayerAction BettingRound1(List<PlayerAction> actions, Card[] hand)
        {
            //order hand first
            Evaluate.SortHand(hand);

            // convert hand to inner class
            refHand = ConvertHandToRefCards(hand);

            // get rank of current hand and highest card
            Card highCard = null;
            int currentRank = Evaluate.RateAHand(hand, out highCard);

            if (currentRank < 2) // check for at least a potential pair
            {
                currentRank = 2;
            }

            // based on current rank, loop through and evaluate all possible hand types and determine most likely
            PossibilityLevel bestChance = PossibilityLevel.Impossible;
            PossibilityLevel currentChance;
            for (int i = currentRank; i < 11; i++)
            {
                currentChance = CheckForHandOfRank(i, refHand); // get chance of current hand

                if (currentChance < bestChance) // if current chance is better than current best, reassign
                {
                    rankOfTargetHands.Clear(); // clear old bests
                    bestChance = currentChance; // overwrite best
                    rankOfTargetHands.Add(i); // add new best hand by their rank
                }
                else if (currentChance == bestChance) // if equally as good
                {
                    rankOfTargetHands.Add(i); // add both target ranks
                }
            }

            // loop through target hand indices determine which is most likely 
            if (rankOfTargetHands.Count > 1)
            {
                var evaluateNumBest = -1; //best hand to have a chance of drawing
                foreach (int targetRank in rankOfTargetHands) // resolve conflict
                {
                    // determine which rank is more likely -- NPAR
                    // determine based on pot size/money total
                    int evaluateNum = targetRank;// how likely this hand is to draw
                    int numOfCard = ChanceOfCardsNeeded(evaluateNum);//number of possible cards that could be drawn to get hand
                    for (int i = 0; i < (int)bestChance; i++)//number that will be thrown away
                    {
                        if (numOfCard - i <= 0)//add extra chances for drawing correct card
                        {
                            evaluateNum *= numOfCard;
                        }
                        else
                        {//have to draw correct cards
                            evaluateNum *= (numOfCard - i);
                        }
                    }

                    // assign target rank
                    if (evaluateNum > evaluateNumBest)//there is a better chance of getting this hand
                    {
                        targetHandRank = targetRank;
                        evaluateNumBest = evaluateNum;
                    }
                    else if (evaluateNum == evaluateNumBest && targetRank > targetHandRank)//same chance of happening and targetRank is better than target hand
                    {
                        targetHandRank = targetRank;
                    }

                }
            }
            else if (rankOfTargetHands.Count == 1)//only one in array, have to go for that one
            {
                targetHandRank = rankOfTargetHands[0];
            }
            else//catch if list is empty
            {
                targetHandRank = 0;
            }

            // get total amount of discards
            int discardingTotal = GetTotalDiscardsForHandRank(targetHandRank);

            // Determine bet/bluff action -- KPAR, NPRO
            Boolean bluffing = determineBluff();

            // return appropriate PlayerAction object
            int amountPlaced = 0;
            string actionTaken = determinePlayerAction(currentRank, actions, bluffing, out amountPlaced);//the player action that will be taken for this turn
            return new PlayerAction(Name, "Bet1", actionTaken, amountPlaced);
        }

        public override PlayerAction BettingRound2(List<PlayerAction> actions, Card[] hand)
        {
            //order hand first
            Evaluate.SortHand(hand);

            // get rank of current hand and highest card
            Card highCard = null;
            int finalRank = Evaluate.RateAHand(hand, out highCard); // final hand rank

            //  Determine bet/bluff action -- KPAR, NPRO
            Boolean bluffing = determineBluff();

            // return appropriate PlayerAction object
            int amountPlaced = 0;
            string actionTaken = determinePlayerAction(finalRank, actions, bluffing, out amountPlaced);//the player action that will be taken for this turn
            return new PlayerAction(Name, "Bet2", actionTaken, amountPlaced);
        }

        public override PlayerAction Draw(Card[] hand) // JHAT
        {
            List<Card> discards = new List<Card>();

            // get card values to be discarded for target hand
            foreach (RefractionCard card in refHand)
            {
                if (card.HandsThatWouldDiscard.Contains(targetHandRank))
                {
                    discards.Add(card.CardValue);
                }
            }

            if (discards.Count == 5) // check if deleting all or none
            {
                // delete them all
                for (int i = 0; i < hand.Length; i++)
                {
                    hand[i] = null;
                }
                return new PlayerAction(Name, "Draw", "draw", 5);
            }
            else if (discards.Count < 1) // no cards deleted
            {
                return new PlayerAction(Name, "Draw", "stand pat", 0);
            }

            int cardsToDelete = discards.Count;

            // remove card(s) from hand
            for (int i = 0; i < hand.Length; i++)
            {
                Card current = hand[i];

                if (discards.Count > 0 && discards.Contains(current)) // if card is marked for discard
                {
                    hand[i] = null; // delete card from hand
                    discards.Remove(current); // remove current to speed up future searches
                }
                else if (discards.Count < 1)
                {
                    break;
                }
            }

            // return PlayerAction object
            return new PlayerAction(Name, "Draw", "draw", cardsToDelete);
        }

        private int GetTotalDiscardsForHandRank(int rank)
        {
            if (rank <= 0)//for some reason no good rank
            {
                return 4;
            }
            int discards = 0;
            foreach (RefractionCard card in refHand) // loop through inner class hand
            {
                if (card.HandsThatWouldDiscard.Contains(rank)) // if card property list contains rank, it would be discarded
                {
                    discards++; // increment total
                }
            }

            return discards;
        }

        private List<RefractionCard> ConvertHandToRefCards(Card[] hand)
        {
            List<RefractionCard> aiHand = new List<RefractionCard>();
            foreach (Card baseCard in hand)
            {
                aiHand.Add(new RefractionCard(baseCard));
            }

            return aiHand;
        }

        // generic method to check for possibility of a specific ranked hand
        private PossibilityLevel CheckForHandOfRank(int rank, List<RefractionCard> hand)
        {
            switch (rank)
            {
                case 2:
                    return CheckForPair(rank, hand);
                case 3:
                    return CheckForTwoPair(rank, hand);
                case 4:
                    return CheckForThreeOfAKind(rank, hand);
                case 5:
                    return CheckForStraight(rank, hand);
                case 6:
                    return CheckForFlush(rank, hand);
                case 7:
                    return CheckForFullHouse(rank, hand);
                case 8:
                    return CheckForFourOfAKind(rank, hand);
                case 9:
                    return CheckForStraightFlush(rank, hand);
                case 10:
                    return CheckForRoyalFlush(rank, hand);
                default:
                    return PossibilityLevel.Impossible;
            }
        }

        private PossibilityLevel CheckForRoyalFlush(int rank, List<RefractionCard> hand)//Rank 10 //Noah
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            string[] suits = { "Hearts", "Clubs", "Diamonds", "Spades" };
            foreach (string suit in suits)
            {
                if (bestSet.Count >= 3) { break; }//no other set can do better then three

                if (hand[0].CardValue.Value == 10 && hand[0].CardValue.Suit == suit)//10
                {
                    currentSet.Add(hand[0]);
                }
                if (hand[1].CardValue.Value == 11 && hand[1].CardValue.Suit == suit)//Jack
                {
                    currentSet.Add(hand[1]);
                }
                if (hand[2].CardValue.Value == 12 && hand[2].CardValue.Suit == suit)//Queen
                {
                    currentSet.Add(hand[2]);
                }
                if (hand[3].CardValue.Value == 13 && hand[3].CardValue.Suit == suit)//King
                {
                    currentSet.Add(hand[3]);
                }
                if (hand[4].CardValue.Value == 14 && hand[4].CardValue.Suit == suit)//Ace
                {
                    currentSet.Add(hand[4]);
                }

                if (currentSet.Count > bestSet.Count)//there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                currentSet.Clear();
            }
            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c))//best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForStraightFlush(int rank, List<RefractionCard> hand)//Rank 9 //Noah
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--)
            {//goes through all 5 cards
                if (bestSet.Count >= i + 1) { break; }//no other set can do better than the one that is already the best
                currentSet.Add(hand[i]);//start off the set
                for (int y = i - 1; y >= 0; y--)//checks each card
                {
                    if (hand[y].CardValue.Value > currentSet[0].CardValue.Value - 5 && hand[y].CardValue.Suit == currentSet[0].CardValue.Suit)//check if card is within range and the same suit
                    {
                        currentSet.Add(hand[y]);
                    }
                }
                if (currentSet.Count > bestSet.Count)//there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                else if (currentSet.Count == bestSet.Count)// there are the same amount of cards in this list, compare highest card
                {
                    if (currentSet[0].CardValue.Value > bestSet[0].CardValue.Value)
                    {
                        bestSet = new List<RefractionCard>(currentSet);
                    }
                }
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c))//best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForFourOfAKind(int rank, List<RefractionCard> hand)//Rank 8 //Kenny
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--) // go through each card and comapre to every other card
            {
                if (bestSet.Count >= 4) { break; } // no better set than 4
                currentSet.Add(hand[i]);//you always have 1 piece of the 4 cards
                for (int y = i - 1; y >= 0; y--)
                {
                    if (hand[i].CardValue.Value == hand[y].CardValue.Value) // we have a match, place BOTH in current set, if not already there
                    {
                        currentSet.Add(hand[y]);
                    }
                }

                if (currentSet.Count > bestSet.Count) // there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                else if (currentSet.Count == bestSet.Count) // there are the same amount of cards in this list, compare highest card
                {
                    if (currentSet[0].CardValue.Value > bestSet[0].CardValue.Value)
                    {
                        bestSet = new List<RefractionCard>(currentSet);
                    }
                }
                // clear current list and go to
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c)) // best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForFullHouse(int rank, List<RefractionCard> hand)//Rank 7 //Kenny
        {
            /// logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet1 = new List<RefractionCard>();
            List<RefractionCard> bestSet2 = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--) // go through each card and comapre to every other card
            {
                if ((bestSet1.Count >= 3 && bestSet2.Count >= 2) || (bestSet1.Count >= 2 && bestSet2.Count >= 3)) { break; } // no better set than 5
                currentSet.Add(hand[i]);//you always have one card of the house
                for (int y = i - 1; y >= 0; y--)
                {
                    if (hand[i].CardValue.Value == hand[y].CardValue.Value) // we have a match, place BOTH in current set, if not already there
                    {
                        currentSet.Add(hand[y]);
                    }
                }

                if ((currentSet.Count > bestSet2.Count) || (currentSet.Count > bestSet1.Count)) // there are more cards in this set then either previous best set
                {
                    // if nothing filled, fill the bottom
                    if (bestSet1.Count == 0 && bestSet2.Count == 0) { bestSet2 = new List<RefractionCard>(currentSet); }

                    // put it in the top "best" if the bottom is filled
                    else if (bestSet1.Count == 0 && bestSet2.Count != 0) { bestSet1 = new List<RefractionCard>(currentSet); }

                    // if both are filled...
                    else if (bestSet1.Count != 0 && bestSet2.Count != 0)
                    {
                        // see which "best" set is smaller, check to see if current is larger than it
                        // bottom best is larger, try to put in top
                        if (bestSet1.Count < bestSet2.Count)
                        {
                            if (currentSet.Count > bestSet1.Count) { bestSet1 = new List<RefractionCard>(currentSet); }
                        }

                        //top best is larger, try to put in bottom
                        if (bestSet1.Count > bestSet2.Count)
                        {
                            if (currentSet.Count > bestSet2.Count) { bestSet2 = new List<RefractionCard>(currentSet); }
                        }
                    }

                }

                else if (currentSet.Count == bestSet1.Count && currentSet.Count == bestSet2.Count) // there are the same amount of cards in both lists, compare highest card
                {
                    // bottom has lower value, place here
                    if (bestSet1[0].CardValue.Value > bestSet2[0].CardValue.Value)
                    {
                        if (currentSet[0].CardValue.Value > bestSet2[0].CardValue.Value) { bestSet2 = new List<RefractionCard>(currentSet); }
                    }

                    // top has lower value, place here
                    if (bestSet1[0].CardValue.Value < bestSet2[0].CardValue.Value)
                    {
                        if (currentSet[0].CardValue.Value > bestSet1[0].CardValue.Value) { bestSet1 = new List<RefractionCard>(currentSet); }
                    }
                }
                // clear current list and go to
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet1.Contains(c) && !bestSet2.Contains(c)) // best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet1.Count - bestSet2.Count); // based on amount of cards discarded return  
        }

        private PossibilityLevel CheckForFlush(int rank, List<RefractionCard> hand)//Rank 6 //Noah
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            string[] suits = { "Hearts", "Clubs", "Diamonds", "Spades" };
            foreach (string suit in suits)
            {
                if (bestSet.Count >= 3) { break; }//no other set can do better then three
                for (int i = 4; i >= 0; i--)//checks each card in hand
                {
                    if (hand[i].CardValue.Suit == suit)//checks to see if the current card is the correct suit
                    {
                        currentSet.Add(hand[i]);
                    }
                }
                if (currentSet.Count > bestSet.Count)//there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                else if (currentSet.Count == bestSet.Count && currentSet.Count != 0)//there are the same amount of cards in this list, and current has cards
                {
                    if (currentSet[0].CardValue.Value > bestSet[0].CardValue.Value)// compare highest card,
                    {
                        bestSet = new List<RefractionCard>(currentSet);
                    }
                }
                currentSet.Clear();
            }
            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c))//best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForStraight(int rank, List<RefractionCard> hand)//Rank 5 //Noah
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--)
            {//goes through all 5 cards
                if (bestSet.Count >= i + 1) { break; }//no other set can do better than the one that is already the best
                currentSet.Add(hand[i]);//start off the set
                for (int y = i - 1; y >= 0; y--)//checks each card
                {
                    if (hand[y].CardValue.Value > currentSet[0].CardValue.Value - 5 && hand[y].CardValue.Value != hand[y + 1].CardValue.Value)//check if card is within range and not a duplicate value
                    {
                        currentSet.Add(hand[y]);
                    }
                }
                if (currentSet.Count > bestSet.Count)//there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c))//best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForThreeOfAKind(int rank, List<RefractionCard> hand)//Rank 4 //Kenny
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--) // go through each card and comapre to every other card
            {
                if (bestSet.Count >= 3) { break; } // no better set than 3
                currentSet.Add(hand[i]);//always have a third of the cards needed
                for (int y = i - 1; y >= 0; y--)
                {
                    if (hand[i].CardValue.Value == hand[y].CardValue.Value) // we have a match, place BOTH in current set, if not already there
                    {
                        currentSet.Add(hand[y]);
                    }
                }

                if (currentSet.Count > bestSet.Count) // there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                else if (currentSet.Count == bestSet.Count) // there are the same amount of cards in this list, compare highest card
                {
                    if (currentSet[0].CardValue.Value > bestSet[0].CardValue.Value)
                    {
                        bestSet = new List<RefractionCard>(currentSet);
                    }
                }
                // clear current list and go to
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c)) // best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForTwoPair(int rank, List<RefractionCard> hand)//Rank 3 //Kenny
        {
            /// logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet1 = new List<RefractionCard>();
            List<RefractionCard> bestSet2 = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--) // go through each card and comapre to every other card
            {
                if (bestSet1.Count >= 2 && bestSet2.Count >= 2) { break; } // no better set than 4
                currentSet.Add(hand[i]);//always have half of the pair
                for (int y = i - 1; y >= 0; y--)
                {
                    if (hand[i].CardValue.Value == hand[y].CardValue.Value) // we have a match, place BOTH in current set, if not already there
                    {
                        currentSet.Add(hand[y]);
                        break;
                    }
                }

                if ((currentSet.Count > bestSet2.Count) || (currentSet.Count > bestSet1.Count)) // there are more cards in this set then either previous best set
                {
                    // if nothing filled, fill the bottom
                    if (bestSet1.Count == 0 && bestSet2.Count == 0) { bestSet2 = new List<RefractionCard>(currentSet); }

                    // put it in the top "best" if the bottom is filled
                    else if (bestSet1.Count == 0 && bestSet2.Count != 0) { bestSet1 = new List<RefractionCard>(currentSet); }

                    // if both are filled...
                    else if (bestSet1.Count != 0 && bestSet2.Count != 0)
                    {
                        // see which "best" set is smaller, check to see if current is larger than it
                        // bottom best is larger, try to put in top
                        if (bestSet1.Count < bestSet2.Count)
                        {
                            if (currentSet.Count > bestSet1.Count) { bestSet1 = new List<RefractionCard>(currentSet); }
                        }

                        //top best is larger, try to put in bottom
                        if (bestSet1.Count > bestSet2.Count)
                        {
                            if (currentSet.Count > bestSet2.Count) { bestSet2 = new List<RefractionCard>(currentSet); }
                        }
                    }
                }

                else if (currentSet.Count == bestSet1.Count && currentSet.Count == bestSet2.Count) // there are the same amount of cards in both lists, compare highest card
                {
                    // bottom has lower value, place here
                    if (bestSet1[0].CardValue.Value > bestSet2[0].CardValue.Value)
                    {
                        if (currentSet[0].CardValue.Value > bestSet2[0].CardValue.Value) { bestSet2 = new List<RefractionCard>(currentSet); }
                    }

                    // top has lower value, place here
                    if (bestSet1[0].CardValue.Value < bestSet2[0].CardValue.Value)
                    {
                        if (currentSet[0].CardValue.Value > bestSet1[0].CardValue.Value) { bestSet1 = new List<RefractionCard>(currentSet); }
                    }
                }
                // clear current list and go to
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet1.Contains(c) && !bestSet2.Contains(c)) // best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet1.Count - bestSet2.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel CheckForPair(int rank, List<RefractionCard> hand)//Rank 2 //Kenny
        {
            // logic that would mark refraction cards for discard in order to possibly get this hand
            List<RefractionCard> bestSet = new List<RefractionCard>();
            List<RefractionCard> currentSet = new List<RefractionCard>();
            for (int i = 4; i >= 0; i--) // go through each card and comapre to every other card
            {
                if (bestSet.Count >= 2) { break; } // no better set than 4
                currentSet.Add(hand[i]);//always have half of the pair
                for (int y = i - 1; y >= 0; y--)
                {
                    if (hand[i].CardValue.Value == hand[y].CardValue.Value) // we have a match, place BOTH in current set, if not already there
                    {
                        currentSet.Add(hand[y]);
                        break;
                    }
                }

                if (currentSet.Count > bestSet.Count) // there are more cards in this set then in previous best set
                {
                    bestSet = new List<RefractionCard>(currentSet);
                }
                else if (currentSet.Count == bestSet.Count) // there are the same amount of cards in this list, compare highest card
                {
                    if (currentSet[0].CardValue.Value > bestSet[0].CardValue.Value)
                    {
                        bestSet = new List<RefractionCard>(currentSet);
                    }
                }
                // clear current list and go to
                currentSet.Clear();
            }

            foreach (RefractionCard c in hand)//assigns what card would be discarded
            {
                if (!bestSet.Contains(c)) // best hand does not contain card
                {
                    c.DiscardFromHandWithRank(rank);
                }
            }

            return GetPossilityForDiscards(5 - bestSet.Count); // based on amount of cards discarded return 
        }

        private PossibilityLevel GetPossilityForDiscards(int numDiscarded)
        {
            switch (numDiscarded)
            {
                case 0:
                    return PossibilityLevel.Have;
                case 1:
                    return PossibilityLevel.Likely;
                case 2:
                    return PossibilityLevel.Possible;
                case 3:
                    return PossibilityLevel.Unlikely;
                case 4:
                    return PossibilityLevel.Poor;
                case 5:
                    return PossibilityLevel.Impossible;
                default:
                    return PossibilityLevel.Impossible;
            }
        }

        private void SortHand(List<RefractionCard> hand)
        {
            // simple bubble sort - with 5 cards almost as fast as other
            // types of sorts
            int n = hand.Count;
            Boolean swapped = false;
            do
            {
                swapped = false; // reset flag for next iteration
                for (int i = 1; i < n; i++)
                {
                    if (hand[i - 1].CardValue.Value > hand[i].CardValue.Value) // do we swap?
                    {
                        RefractionCard temp = hand[i - 1];
                        hand[i - 1] = hand[i];
                        hand[i] = temp;
                        swapped = true;
                    }
                }
                n--; // largest value is at the end of the array
            } while (swapped == true);
        }

        // helper method - see if hand is all the same suit
        // could mean a Flush, Straight Flush, or Royal Flush
        private Boolean SameSuit(List<RefractionCard> hand)
        {
            // are all cards from the same suit
            for (int i = 1; i < hand.Count; i++)
            {
                if (hand[i].CardValue.Suit != hand[0].CardValue.Suit)
                {
                    return false;
                }
            }

            // finished loop - all cards are the same suit
            return true;
        }

        // the Number of cards in the deck that could be used for that hand
        private int ChanceOfCardsNeeded(int rank)
        {
            switch (rank)
            {
                case 2: //pair
                    return 16;
                case 3: //two pair
                    return 12;
                case 4: //Three of a Kind
                    return 12;
                case 5: //Straight //could use some work
                    return 10;
                case 6: //Flush
                    return 14;
                case 7: //Full House
                    return 8;
                case 8: //Four of a Kind
                    return 8;
                case 9: //Straight Flush
                    return 5;
                case 10: //Royal Flush
                    return 5;
                default:
                    return 0;
            }
        }

        //figures out what player action should be done based on hand strength
        private string determinePlayerAction(int handRank, List<PlayerAction> roundActions, Boolean bluffing, out int betAmount)
        {
            betAmount = 0;

            // get last action in the queue
            PlayerAction lastAction = new PlayerAction(Name, "none", "none", 0);
            if (roundActions.Count != 0)
            {
                lastAction = roundActions[roundActions.Count - 1];
            }

            //look at opponent bet if opponent bet first
            //if enemy bet anything
            if (lastAction.Name != Name) // only evaluate if opponent took last action
            {
                switch (lastAction.ActionName)
                {
                    case "bet": // look at lastAction.Amount, do logic
                        return determineResponce(lastAction.Amount, handRank, out betAmount);
                    case "raise":
                        return determineResponce(lastAction.Amount, handRank, out betAmount);
                    case "call":
                        break; //matched your bet, just continue on like normal
                    case "check":
                        break; //just continue on like normal
                }

            }

            betAmount = determineBet(handRank, bluffing, 0);

            //determine choice, update determine action that should be taken if first

            int randAmount = rnd.Next(-2, 2);

            //low chance of folding and not bluffing ex. if junk then 1/4 chance of foldering
            if (handRank + randAmount <= 0 && !bluffing)
            {
                return "fold";
            }
            else if (handRank + randAmount >= 5 && Money >= 10)//bet the amount and makes sure we actually have some money to use
            {
                return bluffing ? "bet" : "check";
            }
            return bluffing ? "check" : "bet";//just match them
        }

        private int determineBet(int handRank, Boolean bluffing, int raiseAmount)//determine the amount that needs that would be bet
        {
            double percent = (rnd.NextDouble() * .4) - .2;
            if (!bluffing)//not bluffing
            {
                switch (handRank)
                {
                    case 5:
                        return (int)((.3 + percent) * Money);
                    case 6:
                        return (int)((.4 + percent) * Money);
                    case 7:
                        return (int)((.5 + percent) * Money);
                    case 8:
                        return (int)((.5 + percent) * Money);
                    case 9:
                        return (int)((.6 + percent) * Money);
                    case 10:
                        return (int)((.7 + percent) * Money);
                    default:
                        return 1;
                }
            }
            else//bluffing
            {
                switch (handRank)
                {
                    case 2:
                        return (int)((.4 + percent) * Money);
                    case 3:
                        return (int)((.4 + percent) * Money);
                    case 4:
                        return (int)((.3 + percent) * Money);
                    case 5:
                        return (int)((.3 + percent) * Money);
                    case 6:
                        return (int)((.2 + percent) * Money);
                    case 7:
                        return (int)((.1 + percent) * Money);
                    default:
                        return 1;
                }
            }
        }

        // function to determine if the AI should bluff or not- based on targetHandRank and a random number
        private bool determineBluff()
        {
            //TODO adjust this

            // have low, bluff for high or have high, bluff for low
            int chanceValue = targetHandRank * 10;
            int chance = rnd.Next(0, chanceValue + 1);
            return chance + chanceValue >= (chanceValue * 3 / 2);
        }

        private string determineResponce(int betRaisedAmount, int handRank, out int betAmount)//if other player bets or raises, determine how to respond
        {
            betAmount = 0;
            int randAmount = rnd.Next(-2, 2);

            //if they bet more then player has
            //low chance of folding ex. if pair then 1/4 chance of foldering
            if (handRank + randAmount < 1 || betRaisedAmount > Money)
            {
                return "fold";
            }
            else if (handRank + randAmount >= 9 && Money >= 10)//raise the amount and makes sure we actually have some money to use
            {
                do//calculates amount to raise until amount is above 0
                {
                    betAmount = (int)(((handRank - 5 + rnd.Next(-2, 2)) / 100) * Money); //when raising the amount will not be much our total amount
                } while (betAmount <= 0);
                return "raise";
            }
            return "call";//just match them
        }

        class RefractionCard // inner class to add property to each card to determine what hands would require it to be discarded
        {
            Card cardValue;
            List<int> handsThatWouldDiscard; // list of ints (hand ranks) that would not want this card

            public RefractionCard(Card baseCard) // store base card object with list reference to hands that would discard
            {
                cardValue = baseCard;
                handsThatWouldDiscard = new List<int>();
            }

            public void DiscardFromHandWithRank(int rank) // function to add hands that would discard this card
            {
                handsThatWouldDiscard.Add(rank);
            }

            /* Properties */
            public List<int> HandsThatWouldDiscard
            {
                get { return handsThatWouldDiscard; }
            }

            public Card CardValue
            {
                get { return cardValue; }
                set { cardValue = value; }
            }
        }
    }
}
