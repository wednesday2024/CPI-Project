using ClubPenguin.Avatar;
using ClubPenguin.ClothingDesigner.Inventory;
using ClubPenguin.Core;
using ClubPenguin.Net;
using ClubPenguin.Net.Domain;
using ClubPenguin.Tubes;
using Disney.Kelowna.Common.DataModel;
using Disney.LaunchPadFramework;
using Disney.MobileNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ClubPenguin
{
    public class ChatActivityService : AbstractDataModelService
    {
        private ChatActivityData chatActivityData;

        private EventDispatcher dispatcher;

        private const string ERROR_TEXT = "Error! That ID does not exist.";
        private const string GRANTED_TUBE = "Granted Tube.";
        private const string GRANTED_EQUIPMENT = "Granted Equipment.";

        private void Start()
        {
            dispatcher = Service.Get<EventDispatcher>();
            DataEntityHandle handle = dataEntityCollection.AddEntity("ChatActivity");
            chatActivityData = dataEntityCollection.AddComponent<ChatActivityData>(handle);
            ChatActivityData obj = chatActivityData;
            obj.OnTimeOutComplete = (System.Action)Delegate.Combine(obj.OnTimeOutComplete, new System.Action(onTimeOutComplete));
            ChatActivityData obj2 = chatActivityData;
            obj2.SendChatActivity = (System.Action)Delegate.Combine(obj2.SendChatActivity, new System.Action(onSendChatActivity));
            dispatcher.AddListener<ChatActivityServiceEvents.SendChatActivity>(onSendChatActivity);
            dispatcher.AddListener<ChatActivityServiceEvents.SendChatActivityCancel>(onSendChatActivityCancel);
            dispatcher.AddListener<ChatMessageSender.SendChatMessage>(onSendChatMessage, EventDispatcher.Priority.FIRST);
        }

        private bool onSendChatActivity(ChatActivityServiceEvents.SendChatActivity evt)
        {
            if (!chatActivityData.IsChatActive)
            {
                dispatcher.DispatchEvent(default(ChatServiceEvents.SendChatActivity));
            }
            chatActivityData.OnSendChatActivity();
            return true;
        }

        private bool onSendChatActivityCancel(ChatActivityServiceEvents.SendChatActivityCancel evt)
        {
            if (chatActivityData.IsChatActive)
            {
                dispatcher.DispatchEvent(default(ChatServiceEvents.SendChatActivityCancel));
            }
            chatActivityData.OnSetChatActiveCancel();
            return true;
        }

        private bool onSendChatMessage(ChatMessageSender.SendChatMessage evt)
        {
            string message = evt.Message != null ? evt.Message.Trim() : "";
            if (message.StartsWith("!"))
            {
                return handleCommand(message);
            }

            if (chatActivityData.IsChatActive)
            {
                dispatcher.DispatchEvent(default(ChatServiceEvents.SendChatActivityCancel));
            }
            chatActivityData.OnSendChatMessage();
            return false;
        }

        private bool handleCommand(string message)
        {
            string[] parts = message.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLower();

            switch (command)
            {
                case "!ac":
                    handleAddCoins(parts);
                    break;
                case "!ae":
                    handleAddItem(parts);
                    break;
                case "!at":
                    handleAddTube(parts);
                    break;
                default:
                    postSystemChatError();
                    break;
            }
            return true;
        }

        private void postSystemChat(string text, bool isError)
        {
            if (dispatcher == null || dataEntityCollection == null)
            {
                return;
            }

            StartCoroutine(postSystemChatRoutine(text, isError));
        }

        private IEnumerator postSystemChatRoutine(string text, bool isError)
        {
            if (dispatcher == null || dataEntityCollection == null)
            {
                yield break;
            }

            string localOnly;
            if (isError)
            {
                localOnly = new string(new char[] { WorldSpeechBubble.SystemMessagePrefix, WorldSpeechBubble.SystemErrorPrefix }) + text;
            }
            else
            {
                localOnly = new string(new char[] { WorldSpeechBubble.SystemMessagePrefix }) + text;
            }

            dispatcher.DispatchEvent(new ChatMessageSender.SendChatMessage(localOnly, null, false));
            yield return null;

            long sessionId = dataEntityCollection.LocalPlayerSessionId;
            dispatcher.DispatchEvent(new ChatServiceEvents.ChatMessageReceived(sessionId, localOnly, 0));
        }

        private void postSystemChatError()
        {
            postSystemChat(ERROR_TEXT, true);
        }

        private void postSystemChatGrantedTube()
        {
            postSystemChat(GRANTED_TUBE, false);
        }

        private void postSystemChatGrantedEquipment()
        {
            postSystemChat(GRANTED_EQUIPMENT, false);
        }

        private void handleAddCoins(string[] parts)
        {
            if (parts.Length < 2)
            {
                postSystemChatError();
                return;
            }

            int amount;
            if (!int.TryParse(parts[1], out amount) || amount <= 0)
            {
                postSystemChatError();
                return;
            }

            CoinsData coinsData;
            if (dataEntityCollection.TryGetComponent(dataEntityCollection.LocalPlayerHandle, out coinsData))
            {
                QARewards.AddCoinsToAccount(amount);
            }
            else
            {
                postSystemChatError();
            }
        }

        private void handleAddItem(string[] parts)
        {
            if (parts.Length < 2)
            {
                postSystemChatError();
                return;
            }

            int definitionId;
            if (!int.TryParse(parts[1], out definitionId))
            {
                postSystemChatError();
                return;
            }

            Dictionary<int, TemplateDefinition> templates = Service.Get<GameData>().Get<Dictionary<int, TemplateDefinition>>();
            TemplateDefinition templateDef;
            if (templates == null || !templates.TryGetValue(definitionId, out templateDef) || templateDef == null)
            {
                postSystemChatError();
                return;
            }

            DCustomEquipment equipment = default(DCustomEquipment);
            equipment.DefinitionId = definitionId;
            equipment.Name = templateDef.AssetName;
            equipment.Parts = new DCustomEquipmentPart[0];
            equipment.DateTimeCreated = DateTime.UtcNow.Ticks;

            CustomEquipment request = CustomEquipmentResponseAdaptor.ConvertCustomEquipmentToRequest(equipment);

            dispatcher.AddListener<InventoryServiceEvents.EquipmentCreated>(onEquipmentCreated);
            Service.Get<INetworkServicesManager>().InventoryService.CreateCustomEquipment(request);
        }

        private void handleAddTube(string[] parts)
        {
            if (parts.Length < 2)
            {
                postSystemChatError();
                return;
            }

            int definitionId;
            if (!int.TryParse(parts[1], out definitionId))
            {
                postSystemChatError();
                return;
            }

            Dictionary<int, TubeDefinition> tubeDefinitions = Service.Get<GameData>().Get<Dictionary<int, TubeDefinition>>();
            if (tubeDefinitions == null)
            {
                postSystemChatError();
                return;
            }

            TubeDefinition tubeDef;
            if (!tubeDefinitions.TryGetValue(definitionId, out tubeDef) || tubeDef == null)
            {
                postSystemChatError();
                return;
            }

            Reward reward = new Reward();
            reward.Add(new TubeReward(definitionId));
            Service.Get<INetworkServicesManager>().RewardService.QA_SetReward(reward);

            postSystemChatGrantedTube();
        }

        private bool onEquipmentCreated(InventoryServiceEvents.EquipmentCreated evt)
        {
            dispatcher.RemoveListener<InventoryServiceEvents.EquipmentCreated>(onEquipmentCreated);

            long equipmentId = evt.EquipmentId;

            DataEntityHandle localPlayerHandle = dataEntityCollection.LocalPlayerHandle;

            InventoryData inventoryData;
            if (dataEntityCollection.TryGetComponent(localPlayerHandle, out inventoryData) && inventoryData.Inventory != null)
            {
                DCustomEquipment createdEquipment = default(DCustomEquipment);
                createdEquipment.Id = equipmentId;
                createdEquipment.DateTimeCreated = DateTime.UtcNow.Ticks;

                InventoryIconModel<DCustomEquipment> iconModel = new InventoryIconModel<DCustomEquipment>(equipmentId, createdEquipment, false, true);
                if (!inventoryData.Inventory.ContainsKey(equipmentId))
                {
                    inventoryData.Inventory.Add(equipmentId, iconModel);
                }
            }

            postSystemChatGrantedEquipment();
            return false;
        }

        private void onTimeOutComplete()
        {
            dispatcher.DispatchEvent(default(ChatServiceEvents.SendChatActivityCancel));
        }

        private void onSendChatActivity()
        {
            Service.Get<EventDispatcher>().DispatchEvent(default(ChatServiceEvents.SendChatActivity));
        }
    }
}