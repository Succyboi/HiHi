using HiHi.Serialization;
using HiHi.Common;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace HiHi {
    public class Question<T> : SyncObject {
        public Action<ushort> OnAsked;
        public Action<ushort, T> OnAnswered;

        public bool AllPeersAnswered => ReceivedAnswers >= expectingAnswers;
        public int ReceivedAnswers => receivedAnswers.Count;
        public int ExpectingAnswers => expectingAnswers;
        /*public T Consensus => receivedAnswers
            .GroupBy(i => i.Value)
            .OrderByDescending(grp => grp.Count())
            .ThenBy(grp => grp.Key)
            .Select(grp => grp.Key)
            .FirstOrDefault();*/

        public T Consensus {
            get {
                IEnumerable<T> answers = receivedAnswers
                    .GroupBy(i => i.Value)
                    .OrderByDescending(grp => grp.Count())
                    .ThenBy(grp => grp.Key)
                    .Select(grp => grp.Key);

                string debugString = string.Empty;

                foreach (T answer in answers) {
                    debugString += $"{answer}, ";
                }

                debugString += $"Picked {answers.FirstOrDefault()}";

                Peer.SendLog(debugString);

                return answers.FirstOrDefault();
            }
        }

        protected override bool RequiresAuthorization => false;

        private Func<T> question;
        private Dictionary<ushort, T> receivedAnswers = new Dictionary<ushort, T>();
        private int expectingAnswers = Peer.Network.PeerIDs.Count;

        public Question(INetworkObject parent, Func<T> question) : base(parent) {
            this.question = question;
        }

        public override void Update() { }

        public void Ask(ushort? destinationPeer = null) {
            ClearReceivedAnswers();

            PeerMessage message = NewMessage(destinationPeer);

            message.Buffer.AddBool(true);

            Peer.SendMessage(message);
        }

        public void Answer(ushort? destinationPeerID = null) {
            PeerMessage message = NewMessage(destinationPeerID);

            message.Buffer.AddBool(false);
            SerializationHelper.Serialize(question.Invoke(), message.Buffer);

            Peer.SendMessage(message);
        }

        public void AnswerSelf() {
            T answer = question.Invoke();

            expectingAnswers++;
            receivedAnswers.Add(Peer.Info.UniqueID, answer);
            OnAnswered?.Invoke(Peer.Info.UniqueID, answer);
        }

        public void ClearReceivedAnswers() {
            receivedAnswers.Clear();
            expectingAnswers = Peer.Network.Connections;
        }

        public override void Synchronize(ushort? destinationPeerID = null) {
            throw new HiHiException($"Question doesn't use {nameof(Synchronize)}. Instead use {nameof(Ask)} and {nameof(Answer)}.");
        }

        public override void Serialize(BitBuffer buffer) {
            base.Serialize(buffer);
        }

        public override void Deserialize(ushort senderPeerID, BitBuffer buffer) {
            bool isQuestion = buffer.ReadBool();

            if (isQuestion) {
                OnAsked?.Invoke(senderPeerID);

                Answer();
            }
            else {
                T receivedAnswer = SerializationHelper.Deserialize<T>(default, buffer);

                receivedAnswers.Add(senderPeerID, receivedAnswer);
                OnAnswered?.Invoke(senderPeerID, receivedAnswer);
            }

            base.Deserialize(senderPeerID, buffer);
        }
    }
}
