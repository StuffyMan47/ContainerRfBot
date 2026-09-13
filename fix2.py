import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'r') as f:
    text = f.read()

# Replace SendMessageAsync(chatId: chatId, ...) with SendMessageToUserAsync(userId: user.Id, ...)
text = text.replace("SendMessageAsync(chatId: chatId,", "SendMessageToUserAsync(userId: user.Id,")
text = text.replace("SendMessageAsync(\n                    chatId: chatId,", "SendMessageToUserAsync(\n                    userId: user.Id,")
text = text.replace("SendMessageAsync(\n                        chatId: chatId,", "SendMessageToUserAsync(\n                        userId: user.Id,")
text = text.replace("SendMessageAsync(\n            chatId: chatId,", "SendMessageToUserAsync(\n            userId: user.Id,")
text = text.replace("chatId: targetId", "userId: targetId")
text = text.replace("SendMessageAsync(userId: targetId,", "SendMessageToUserAsync(userId: targetId,")

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'w') as f:
    f.write(text)
