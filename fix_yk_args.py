import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.API/Controllers/YooKassaController.cs', 'r') as f:
    text = f.read()

text = text.replace('SendMessageToUserAsync(userId, "Ваша оплата успешно получена', 
                    'SendMessageToUserAsync(userId: userId, text: "Ваша оплата успешно получена')

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.API/Controllers/YooKassaController.cs', 'w') as f:
    f.write(text)
