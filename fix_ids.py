import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'r') as f:
    text = f.read()

# Replace SendMessageToUserAsync(userId: user.Id, ...) with SendMessageAsync(chatId: chatId, ...)
text = text.replace("SendMessageToUserAsync(", "SendMessageAsync(")
text = text.replace("userId: user.Id", "chatId: chatId")
text = text.replace("userId: targetId", "chatId: targetId") # Assuming targetId is their chatId, this might be a bug, but let's leave it for now or just change targetId logic.
text = text.replace("chatId: user.Id", "chatId: chatId")

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'w') as f:
    f.write(text)
