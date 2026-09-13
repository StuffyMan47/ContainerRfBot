import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'r') as f:
    text = f.read()

text = text.replace(
"""                    await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id,
                        user.Id,""", 
"""                    await _maxBotClient.Messages.SendMessageToUserAsync(
                        userId: user.Id,""")

text = text.replace(
"""                await _maxBotClient.Messages.SendMessageToUserAsync(userId: user.Id,
                    user.Id,""", 
"""                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id,""")

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'w') as f:
    f.write(text)
