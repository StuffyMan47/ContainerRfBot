import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.API/Controllers/YooKassaController.cs', 'r') as f:
    text = f.read()

text = text.replace("SendMessageAsync(userId", "SendMessageToUserAsync(userId")

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.API/Controllers/YooKassaController.cs', 'w') as f:
    f.write(text)
