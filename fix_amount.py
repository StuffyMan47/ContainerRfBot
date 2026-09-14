import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'r') as f:
    text = f.read()

# Change from 1000m to 300m to match the logs, just in case, though it's just an amount.
text = text.replace("1000m, // Пример суммы", "300m, // Пример суммы")

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'w') as f:
    f.write(text)
