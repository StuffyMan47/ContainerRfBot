import re

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'r') as f:
    text = f.read()

# Add "Назад" to profile
profile_replacement = """                    Buttons = new[]
                    {
                        new[]
                        {
                            new InlineKeyboardButton
                            {
                                Text = "Изменить номер телефона",
                                Type = ButtonType.Callback,
                                Payload = "change_phone"
                            }
                        },
                        new[]
                        {
                            new InlineKeyboardButton
                            {
                                Text = "Назад",
                                Type = ButtonType.Callback,
                                Payload = "back_to_main"
                            }
                        }
                    }"""
text = re.sub(r'Buttons = new\[\]\s*\{\s*new\[\]\s*\{\s*new InlineKeyboardButton\s*\{\s*Text = "Изменить номер телефона",\s*Type = ButtonType\.Callback,\s*Payload = "change_phone"\s*\}\s*\}\s*\}', profile_replacement, text)

# Add "Назад" to instruction
instruction_replacement = """            case "instruction":
                var instrKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Назад", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Здесь будет инструкция по использованию сервиса...", 
                    keyboard: instrKb,
                    cancellationToken: cancellationToken);
                break;"""
text = re.sub(r'case "instruction":\s*await _maxBotClient\.Messages\.SendMessageToUserAsync\(userId: user\.Id, "Здесь будет инструкция по использованию сервиса\.\.\.", cancellationToken: cancellationToken\);\s*break;', instruction_replacement, text)

# Add "Отмена" to create_ad
create_ad_replacement = """            case "create_ad":
                user.State = BotState.WaitingForAdDetails;
                await _userRepository.UpdateAsync(user, cancellationToken);
                
                var cancelAdKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Отмена", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Пожалуйста, напишите характеристики контейнера (тип, цена, город продажи, состояние и т.д.):", 
                    keyboard: cancelAdKb,
                    cancellationToken: cancellationToken);
                break;"""
text = re.sub(r'case "create_ad":\s*user\.State = BotState\.WaitingForAdDetails;\s*await _userRepository\.UpdateAsync\(user, cancellationToken\);\s*await _maxBotClient\.Messages\.SendMessageToUserAsync\(userId: user\.Id, "Пожалуйста, напишите характеристики контейнера \(тип, цена, город продажи, состояние и т\.д\.\):", cancellationToken: cancellationToken\);\s*break;', create_ad_replacement, text)

# Add "Отмена" to change_phone
change_phone_replacement = """            case "change_phone":
                user.State = BotState.WaitingForPhone;
                await _userRepository.UpdateAsync(user, cancellationToken);
                
                var cancelPhoneKb = new InlineKeyboard
                {
                    Buttons = new[]
                    {
                        new[] { new InlineKeyboardButton { Text = "Отмена", Type = ButtonType.Callback, Payload = "back_to_main" } }
                    }
                };
                
                await _maxBotClient.Messages.SendMessageToUserAsync(
                    userId: user.Id, 
                    "Пожалуйста, введите ваш новый номер телефона:", 
                    keyboard: cancelPhoneKb,
                    cancellationToken: cancellationToken);
                break;"""
text = re.sub(r'case "change_phone":\s*user\.State = BotState\.WaitingForPhone;\s*await _userRepository\.UpdateAsync\(user, cancellationToken\);\s*await _maxBotClient\.Messages\.SendMessageToUserAsync\(userId: user\.Id, "Пожалуйста, введите ваш новый номер телефона:", cancellationToken: cancellationToken\);\s*break;', change_phone_replacement, text)

# Add back_to_main handler
back_to_main_case = """            case "back_to_main":
                if (user.State != BotState.None)
                {
                    user.State = BotState.None;
                    await _userRepository.UpdateAsync(user, cancellationToken);
                }
                await SendMainMenuAsync(user, cancellationToken);
                break;
                
            case "admin_confirm_payment":"""
text = text.replace('case "admin_confirm_payment":', back_to_main_case)

with open('/Users/marselfazleev/dev/CSharp/Projects/ContainerRfBot/ContainerRfBot.Bot/Services/MaxBot/MaxBotService.cs', 'w') as f:
    f.write(text)
