using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Ginger.Services;

namespace Ginger.Integration;

using CharacterInstance = Backyard.CharacterInstance;
using GroupInstance = Backyard.GroupInstance;
using ChatInstance = Backyard.ChatInstance;

public static class BackupUtil
{
    public class FullBackupData
    {
        public FaradayCardV4[]? characterCards;
        public List<ImageData>? images;
        public List<ImageData>? backgrounds;
        public List<BackupData.Chat>? chats;
        public UserData? userInfo;
        public ImageData? userPortrait;
        public string? displayName;

        public class ImageData
        {
            public int characterIndex;
            public string filename = "";
            public byte[]? data;
            public string ext => Utility.GetFileExt(filename);
        }

        public bool hasModelSettings => chats != null && chats.Any(c => c.parameters != null);
    }

    public static Backyard.Error CreateBackup(CharacterInstance characterInstance, out FullBackupData? backupInfo)
    {
        if (!Backyard.ConnectionEstablished)
        {
            backupInfo = null;
            return Backyard.Error.NotConnected;
        }

        var error = Backyard.Database.ImportCharacter(characterInstance.instanceId, out var card, out var images, out var userInfo);
        if (error != Backyard.Error.NoError)
        {
            backupInfo = null;
            return error;
        }

        ChatInstance[]? chatInstances = null;
        if (characterInstance.groupId != null)
        {
            error = Backyard.Database.GetChats(characterInstance.groupId, out chatInstances);
            if (error != Backyard.Error.NoError)
            {
                backupInfo = null;
                return error;
            }
        }

        string[] imageUrls = images
            .Where(i => i.imageType == AssetFile.AssetType.Icon)
            .Select(i => i.imageUrl)
            .ToArray();
        string[] backgroundUrls = images
            .Where(i => i.imageType == AssetFile.AssetType.Background)
            .Select(i => i.imageUrl)
            .ToArray();
        string? userImageUrl = images
            .Where(i => i.imageType == AssetFile.AssetType.UserIcon)
            .Select(i => i.imageUrl)
            .FirstOrDefault();

        if (!AppSettings.BackyardLink.BackupUserPersona)
        {
            userInfo = null;
            userImageUrl = null;
        }

        // Create backup
        backupInfo = new FullBackupData
        {
            characterCards = new[] { card?.ToFaradayCard() },
            displayName = card?.data?.displayName,
            userInfo = userInfo,
        };

        if (chatInstances != null)
        {
            backupInfo.chats = chatInstances
                .Select(c =>
                {
                    string? bgName = null;
                    if (c.hasBackground)
                    {
                        int index = Array.FindIndex(backgroundUrls, url =>
                            string.Compare(url, c.staging?.background?.imageUrl, StringComparison.OrdinalIgnoreCase) == 0);
                        if (index != -1)
                            bgName = $"background_{index + 1:00}.{Utility.GetFileExt(c.staging?.background?.imageUrl ?? "")}";
                    }

                    var participants = c.participants
                        .Select(id => Backyard.Database.GetCharacter(id))
                        .Where(cc => cc.instanceId != null)
                        .OrderBy(cc => cc.isCharacter)
                        .ToList();

                    return new BackupData.Chat
                    {
                        name = c.name,
                        history = c.history,
                        staging = c.staging,
                        parameters = AppSettings.BackyardLink.BackupModelSettings ? c.parameters : null,
                        creationDate = c.creationDate.ToUniversalTime(),
                        updateDate = c.updateDate.ToUniversalTime(),
                        backgroundName = bgName,
                        participants = participants.Select(cc => cc.name).ToArray(),
                    };
                })
                .ToList();
        }
        else
        {
            backupInfo.chats = new List<BackupData.Chat>();
        }

        // Images
        backupInfo.images = imageUrls
            .Select(url => new FullBackupData.ImageData
            {
                characterIndex = 0,
                filename = Path.GetFileName(url),
                data = Utility.LoadFile(url),
            })
            .Where(i => i.data != null && i.data.Length > 0)
            .ToList();

        // Background images
        backupInfo.backgrounds = backgroundUrls
            .Select(url => new FullBackupData.ImageData
            {
                filename = Path.GetFileName(url),
                data = Utility.LoadFile(url),
            })
            .Where(i => i.data != null && i.data.Length > 0)
            .ToList();

        // User image
        if (!string.IsNullOrEmpty(userImageUrl) && File.Exists(userImageUrl))
        {
            string imageName = $"user.{Utility.GetFileExt(userImageUrl)}";
            backupInfo.userPortrait = new FullBackupData.ImageData
            {
                filename = imageName,
                data = Utility.LoadFile(userImageUrl),
            };
        }

        return Backyard.Error.NoError;
    }

    public static Backyard.Error CreateBackup(GroupInstance groupInstance, out FullBackupData? backupInfo)
    {
        if (!Backyard.ConnectionEstablished)
        {
            backupInfo = null;
            return Backyard.Error.NotConnected;
        }

        if (!BackyardValidation.CheckFeature(BackyardValidation.Feature.GroupChat))
        {
            backupInfo = null;
            return Backyard.Error.UnsupportedFeature;
        }

        var error = Backyard.Database.ImportParty(groupInstance.instanceId, out var cards, out var characterInstances, out var images, out var userInfo);
        if (error != Backyard.Error.NoError)
        {
            backupInfo = null;
            return error;
        }

        error = Backyard.Database.GetChats(groupInstance.instanceId, out var chatInstances);
        if (error != Backyard.Error.NoError)
        {
            backupInfo = null;
            return error;
        }

        var imageUrls = images
            .Where(i => i.imageType == AssetFile.AssetType.Icon)
            .Select(i => new
            {
                index = Array.FindIndex(characterInstances, c => c.instanceId == i.associatedInstanceId),
                url = i.imageUrl,
            }).ToArray();
        string[] backgroundUrls = images
            .Where(i => i.imageType == AssetFile.AssetType.Background)
            .Select(i => i.imageUrl)
            .ToArray();
        string? userImageUrl = images
            .Where(i => i.imageType == AssetFile.AssetType.UserIcon)
            .Select(i => i.imageUrl)
            .FirstOrDefault();

        if (!AppSettings.BackyardLink.BackupUserPersona)
        {
            userInfo = null;
            userImageUrl = null;
        }

        // Create backup
        backupInfo = new FullBackupData
        {
            characterCards = cards?.Select(c => c.ToFaradayCard()).ToArray(),
            displayName = cards?[0]?.data?.displayName,
            userInfo = userInfo,
        };

        if (chatInstances != null)
        {
            backupInfo.chats = chatInstances
                .Select(c =>
                {
                    string? bgName = null;
                    if (c.hasBackground)
                    {
                        int index = Array.FindIndex(backgroundUrls, url =>
                            string.Compare(url, c.staging?.background?.imageUrl, StringComparison.OrdinalIgnoreCase) == 0);
                        if (index != -1)
                            bgName = $"background_{index + 1:00}.{Utility.GetFileExt(c.staging?.background?.imageUrl ?? "")}";
                    }

                    var participants = c.participants
                        .Select(id => Backyard.Database.GetCharacter(id))
                        .Where(cc => cc.instanceId != null)
                        .OrderBy(cc => cc.isCharacter)
                        .ToList();

                    return new BackupData.Chat
                    {
                        name = c.name,
                        history = c.history,
                        staging = c.staging,
                        parameters = AppSettings.BackyardLink.BackupModelSettings ? c.parameters : null,
                        creationDate = c.creationDate,
                        updateDate = c.updateDate,
                        backgroundName = bgName,
                        participants = participants.Select(cc => cc.name).ToArray(),
                    };
                })
                .ToList();
        }
        else
        {
            backupInfo.chats = new List<BackupData.Chat>();
        }

        // Images
        backupInfo.images = imageUrls
            .Select(i => new FullBackupData.ImageData
            {
                characterIndex = i.index,
                filename = Path.GetFileName(i.url),
                data = Utility.LoadFile(i.url),
            })
            .ToList();

        // Background images
        backupInfo.backgrounds = backgroundUrls
            .Distinct()
            .Select(url => new FullBackupData.ImageData
            {
                filename = Path.GetFileName(url),
                data = Utility.LoadFile(url),
            })
            .ToList();

        // User image
        if (!string.IsNullOrEmpty(userImageUrl) && File.Exists(userImageUrl))
        {
            string imageName = $"user.{Utility.GetFileExt(userImageUrl)}";
            backupInfo.userPortrait = new FullBackupData.ImageData
            {
                filename = imageName,
                data = Utility.LoadFile(userImageUrl),
            };
        }

        return Backyard.Error.NoError;
    }

    private class CharData
    {
        public string name = "";
        public byte[]? portraitData;
        public byte[]? nonPNGImageData;
        public byte[]? cardBytes;
        public string nonPNGImageExt = "png";
    }

    public static FileUtil.Error WriteBackup(string filename, FullBackupData backup)
    {
        if (backup.characterCards == null)
            return FileUtil.Error.NoDataFound;

        var lsCharData = new List<CharData>();

        for (int iChar = 0; iChar < backup.characterCards.Length; ++iChar)
        {
            var charCard = backup.characterCards[iChar];
            if (charCard == null) continue;

            var charData = new CharData
            {
                name = Utility.FirstNonEmpty(charCard.data?.name, Constants.DefaultCharacterName)
            };

            try
            {
                var charImages = backup.images?.Where(i => i.characterIndex == iChar).ToList() ?? new List<FullBackupData.ImageData>();
                if (charImages.Count > 0 && charImages[0].data != null)
                {
                    string ext = Utility.GetFileExt(charImages[0].filename);
                    if (Utility.IsWebP(charImages[0].data))
                    {
                        charData.nonPNGImageData = charImages[0].data;
                        charData.nonPNGImageExt = "webp";
                        charData.portraitData = ConvertToPng(charImages[0].data);
                    }
                    else if (ext == "png")
                    {
                        charData.portraitData = charImages[0].data;
                    }
                    else
                    {
                        charData.nonPNGImageData = charImages[0].data;
                        charData.nonPNGImageExt = ext;
                        charData.portraitData = ConvertToPng(charImages[0].data);
                    }
                }

                charData.portraitData ??= DefaultPortrait.Image;

                // Write json to PNG
                string faradayJson = charCard.ToJson() ?? "";
                var faradayBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(faradayJson));

                // Write PNG with embedded metadata
                charData.cardBytes = FileUtil.WriteExifMetaDataToBytes(charData.portraitData!, "chara", faradayBase64);
                if (charData.cardBytes == null || charData.cardBytes.Length == 0)
                    return FileUtil.Error.FileWriteError;

                lsCharData.Add(charData);
            }
            catch (IOException e)
            {
                if (e.HResult == unchecked((int)0x80070070) || e.HResult == unchecked((int)0x80070027))
                    return FileUtil.Error.DiskFullError;
                return FileUtil.Error.FileWriteError;
            }
            catch
            {
                return FileUtil.Error.UnknownError;
            }
        }

        try
        {
            // Write chat logs
            var chats = new List<KeyValuePair<string, string>>();
            foreach (var backupChat in (backup.chats ?? new List<BackupData.Chat>()).OrderBy(c => c.creationDate))
            {
                // Backyard-compatible format (JSON)
                {
                    string chatFilename = $"chatLog_{backup.displayName}_{backupChat.creationDate.ToUnixTimeSeconds()}.json".Replace(" ", "_");
                    var chatBackup = ConvertToBackyardChatBackup(backupChat);
                    string? json = chatBackup?.ToJson();
                    if (json != null)
                        chats.Add(new KeyValuePair<string, string>(chatFilename, json));
                }

                // Ginger format (LOG)
                {
                    string chatFilename = $"chatLog_{backup.displayName}_{backupChat.creationDate.ToUnixTimeSeconds()}.log".Replace(" ", "_");
                    var gingerChat = ConvertToGingerChat(backupChat);
                    string? json = gingerChat?.ToJson();
                    if (json != null)
                        chats.Add(new KeyValuePair<string, string>(chatFilename, json));
                }
            }

            // Create zip archive
            bool bSoloCharacter = lsCharData.Count == 1;
            var intermediateFilename = Path.GetTempFileName();
            using (var zip = ZipFile.Open(intermediateFilename, ZipArchiveMode.Update, Encoding.ASCII))
            {
                for (int iCard = 0; iCard < lsCharData.Count; ++iCard)
                {
                    var charData = lsCharData[iCard];

                    // Write card png
                    string cardEntryName = bSoloCharacter ? "card.png" : $"card_{iCard:00}_{charData.name}.png";
                    var cardEntry = zip.CreateEntry(cardEntryName, CompressionLevel.NoCompression);
                    using (var writer = cardEntry.Open())
                    {
                        if (charData.cardBytes != null)
                            writer.Write(charData.cardBytes, 0, charData.cardBytes.Length);
                    }

                    // Write non-png portrait (situational)
                    if (charData.nonPNGImageData != null && charData.nonPNGImageData.Length > 0)
                    {
                        string entryName = bSoloCharacter
                            ? $"images/portrait.{charData.nonPNGImageExt}"
                            : $"images/{iCard:00}/portrait.{charData.nonPNGImageExt}";
                        var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                        using (var writer = fileEntry.Open())
                        {
                            writer.Write(charData.nonPNGImageData, 0, charData.nonPNGImageData.Length);
                        }
                    }

                    // Write image files
                    var charImages = backup.images?.Where(i => i.characterIndex == iCard).ToList() ?? new List<FullBackupData.ImageData>();
                    for (int i = 1; i < charImages.Count; ++i)
                    {
                        if (charImages[i].data == null || charImages[i].data!.Length == 0)
                            continue;

                        string ext = Utility.GetFileExt(charImages[i].filename);
                        string entryName = bSoloCharacter
                            ? $"images/image_{i:00}.{ext}"
                            : $"images/{iCard:00}/image_{i:00}.{ext}";
                        var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                        using (var writer = fileEntry.Open())
                        {
                            writer.Write(charImages[i].data!, 0, charImages[i].data!.Length);
                        }
                    }
                }

                // Write background files
                for (int i = 0; i < (backup.backgrounds?.Count ?? 0); ++i)
                {
                    var bg = backup.backgrounds![i];
                    if (bg.data == null || bg.data.Length == 0)
                        continue;

                    string ext = Utility.GetFileExt(bg.filename);
                    string entryName = $"backgrounds/background_{i + 1:00}.{ext}";
                    var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                    using (var writer = fileEntry.Open())
                    {
                        writer.Write(bg.data, 0, bg.data.Length);
                    }
                }

                // Write chat files
                for (int i = 0; i < chats.Count; ++i)
                {
                    string entryName = $"logs/{Utility.ValidFilename(chats[i].Key)}";
                    var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                    using (var writer = fileEntry.Open())
                    {
                        byte[] textBuffer = Encoding.UTF8.GetBytes(chats[i].Value);
                        writer.Write(textBuffer, 0, textBuffer.Length);
                    }
                }

                // Write user data
                if (backup.userInfo != null)
                {
                    // User info
                    if (!string.IsNullOrEmpty(backup.userInfo.persona))
                    {
                        string entryName = "user/user.json";
                        var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                        using (var writer = fileEntry.Open())
                        {
                            byte[] textBuffer = Encoding.UTF8.GetBytes(backup.userInfo.ToJson() ?? "");
                            writer.Write(textBuffer, 0, textBuffer.Length);
                        }
                    }

                    // User portrait
                    if (backup.userPortrait?.data != null && backup.userPortrait.data.Length > 0)
                    {
                        string entryName = $"user/{backup.userPortrait.filename}";
                        var fileEntry = zip.CreateEntry(entryName, CompressionLevel.NoCompression);
                        using (var writer = fileEntry.Open())
                        {
                            writer.Write(backup.userPortrait.data, 0, backup.userPortrait.data.Length);
                        }
                    }
                }
            }

            // Rename temporary file to target file
            if (File.Exists(filename))
                File.Delete(filename);
            File.Move(intermediateFilename, filename);
            return FileUtil.Error.NoError;
        }
        catch
        {
            return FileUtil.Error.UnknownError;
        }
    }

    public static FileUtil.Error ReadBackup(string filename, out FullBackupData? backup)
    {
        var lsCharacterCards = new List<FaradayCardV4>();
        var images = new List<FullBackupData.ImageData>();
        var backgrounds = new List<FullBackupData.ImageData>();
        var userImages = new List<FullBackupData.ImageData>();
        var chats = new Dictionary<string, BackupData.Chat>();
        UserData? userInfo = null;

        try
        {
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (entry.Name == "")
                    continue; // Skip folder entries

                string entryPath = Path.GetDirectoryName(entry.FullName)?.Replace('\\', '/') ?? "";
                string entryFullName = Path.GetFileName(entry.FullName);
                string entryName = Path.GetFileNameWithoutExtension(entry.FullName);
                string entryExt = Utility.GetFileExt(entry.FullName);

                // Read character png
                if (entryPath == "" && entryExt == "png")
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);

                        var extractedData = FileUtil.ExtractJsonFromPNG(buffer);
                        string? extractedJson = extractedData.faraday ?? extractedData.chara;
                        if (!string.IsNullOrEmpty(extractedJson) && FaradayCardV4.Validate(extractedJson))
                        {
                            var characterCard = FaradayCardV4.FromJson(extractedJson);
                            if (characterCard != null)
                            {
                                lsCharacterCards.Add(characterCard);
                                images.Add(new FullBackupData.ImageData
                                {
                                    characterIndex = lsCharacterCards.Count - 1,
                                    filename = entryFullName,
                                    data = buffer,
                                });
                                continue;
                            }
                        }
                    }
                }

                // Read chat log (Backyard json)
                if ((entryPath == "chats" || entryPath == "logs") && entryExt == "json")
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        string chatJson = Encoding.UTF8.GetString(buffer);

                        var chatBackup = Models.Formats.ChatLogs.BackyardChatBackupV2.FromJson(chatJson);
                        if (chatBackup != null)
                        {
                            var chat = ConvertFromBackyardChatBackup(chatBackup);
                            chat.name ??= ChatInstance.DefaultName;
                            chats.TryAdd(entryName.ToLowerInvariant(), chat);
                        }
                        else
                        {
                            var chatBackupV1 = Models.Formats.ChatLogs.BackyardChatBackupV1.FromJson(chatJson);
                            if (chatBackupV1 != null)
                            {
                                var chat = ConvertFromBackyardChatBackupV1(chatBackupV1);
                                chat.name ??= ChatInstance.DefaultName;
                                chats.TryAdd(entryName.ToLowerInvariant(), chat);
                            }
                        }
                    }
                    continue;
                }

                // Read chat log (Ginger log)
                if ((entryPath == "chats" || entryPath == "logs") && entryExt == "log")
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        string chatJson = Encoding.UTF8.GetString(buffer);

                        var gingerChat = Models.Formats.ChatLogs.GingerChatV2.FromJson(chatJson);
                        if (gingerChat != null)
                        {
                            entryName = Path.GetFileNameWithoutExtension(entryName);
                            var chat = ConvertFromGingerChat(gingerChat);
                            chats[entryName.ToLowerInvariant()] = chat;
                        }
                        else
                        {
                            var gingerChatV1 = Models.Formats.ChatLogs.GingerChatV1.FromJson(chatJson);
                            if (gingerChatV1 != null)
                            {
                                entryName = Path.GetFileNameWithoutExtension(entryName);
                                var chat = ConvertFromGingerChatV1(gingerChatV1);
                                chats[entryName.ToLowerInvariant()] = chat;
                            }
                        }
                    }
                    continue;
                }

                // Images
                if (entryPath.StartsWith("images") && Utility.IsSupportedImageFileExt(entryExt))
                {
                    int characterIndex = 0;

                    if (entryPath.Length >= 7)
                    {
                        string imagePath = entryPath.Substring(7);
                        if (imagePath.Length > 0)
                        {
                            int pos_slash = imagePath.IndexOf('/');
                            if (pos_slash != -1)
                                int.TryParse(imagePath.Substring(0, pos_slash), out characterIndex);
                            else
                                int.TryParse(imagePath, out characterIndex);
                        }
                    }

                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        images.Add(new FullBackupData.ImageData
                        {
                            characterIndex = characterIndex,
                            filename = entryFullName,
                            data = buffer,
                        });
                    }
                    continue;
                }

                // Backgrounds
                if (entryPath == "backgrounds" && Utility.IsSupportedImageFileExt(entryExt))
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        backgrounds.Add(new FullBackupData.ImageData
                        {
                            filename = entryFullName,
                            data = buffer,
                        });
                    }
                    continue;
                }

                // User info
                if (entryPath == "user" && entryExt == "json")
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        string chatJson = Encoding.UTF8.GetString(buffer);

                        if (UserData.Validate(chatJson))
                            userInfo = UserData.FromJson(chatJson);
                    }
                    continue;
                }

                // User portrait
                if (entryPath == "user" && Utility.IsSupportedImageFileExt(entryExt))
                {
                    long dataSize = entry.Length;
                    if (dataSize > 0)
                    {
                        using var dataStream = entry.Open();
                        byte[] buffer = new byte[dataSize];
                        dataStream.Read(buffer, 0, (int)dataSize);
                        userImages.Add(new FullBackupData.ImageData
                        {
                            filename = entryFullName,
                            data = buffer,
                        });
                    }
                }
            }

            if (lsCharacterCards.Count == 0)
            {
                backup = null;
                return FileUtil.Error.NoDataFound;
            }

            if (images.Count > 0)
            {
                for (int i = 0; i < lsCharacterCards.Count; ++i)
                {
                    int portraitIndex = images.FindIndex(img => img.characterIndex == i);
                    if (portraitIndex == -1)
                        continue;

                    // images/portrait.*, images/image_00.* supersedes png
                    int idxPortrait = images.FindIndex(img =>
                    {
                        string fn = Path.GetFileNameWithoutExtension(img.filename).ToLowerInvariant();
                        return (img.characterIndex == i) && (fn == "portrait" || fn == "image_00");
                    });

                    if (idxPortrait > portraitIndex)
                    {
                        // Move to front (remove existing)
                        var image = images[idxPortrait];
                        images.RemoveAt(idxPortrait);
                        images.RemoveAt(portraitIndex);
                        images.Insert(portraitIndex, image);
                    }
                }
            }

            backup = new FullBackupData
            {
                characterCards = lsCharacterCards.ToArray(),
                displayName = lsCharacterCards[0].data?.displayName,
                chats = chats.Values.ToList(),
                images = images,
                backgrounds = backgrounds,
                userInfo = userInfo,
                userPortrait = userImages.FirstOrDefault(),
            };
            return FileUtil.Error.NoError;
        }
        catch
        {
            backup = null;
            return FileUtil.Error.FileReadError;
        }
    }

    public static bool CheckIfGroup(string filename, out int count)
    {
        try
        {
            count = 0;
            using var fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(fs, ZipArchiveMode.Read);

            foreach (var entry in archive.Entries)
            {
                if (entry.Name == "")
                    continue;

                string entryPath = Path.GetDirectoryName(entry.FullName)?.Replace('\\', '/') ?? "";
                string entryExt = Utility.GetFileExt(entry.FullName);

                if (entryPath == "" && entryExt == "png")
                    count++;
            }

            return count > 1;
        }
        catch
        {
            count = 0;
            return false;
        }
    }

    /// <summary>
    /// Convert any image format to PNG using SkiaSharp.
    /// </summary>
    private static byte[]? ConvertToPng(byte[]? imageData)
    {
        if (imageData == null || imageData.Length == 0)
            return null;

        using var bitmap = ImageService.LoadImage(imageData);
        if (bitmap == null)
            return null;

        return ImageService.SaveToPng(bitmap);
    }

    /// <summary>
    /// Convert Ginger.BackupData.Chat to local BackupData.Chat for use with chat log format classes.
    /// </summary>
    private static Models.Formats.ChatLogs.BackupData.Chat ToLocalChat(BackupData.Chat source)
    {
        var localChat = new Models.Formats.ChatLogs.BackupData.Chat
        {
            name = source.name,
            creationDate = source.creationDate,
            updateDate = source.updateDate,
            backgroundName = source.backgroundName,
            participants = source.participants,
        };

        if (source.history != null)
        {
            localChat.history = new Models.Formats.ChatLogs.ChatHistory
            {
                name = source.history.name,
                messages = source.history.messages?.Select(m => new Models.Formats.ChatLogs.ChatHistory.Message
                {
                    speaker = m.speaker,
                    creationDate = m.creationDate,
                    updateDate = m.updateDate,
                    activeSwipe = m.activeSwipe,
                    swipes = m.swipes,
                }).ToArray() ?? Array.Empty<Models.Formats.ChatLogs.ChatHistory.Message>(),
            };
        }

        if (source.staging != null)
        {
            localChat.staging = new Models.Formats.ChatLogs.ChatStaging
            {
                system = source.staging.system ?? "",
                scenario = source.staging.scenario ?? "",
                greeting = Models.Formats.ChatLogs.CharacterMessage.FromString(source.staging.greeting.text ?? ""),
                example = source.staging.example ?? "",
                grammar = source.staging.grammar ?? "",
                authorNote = source.staging.authorNote ?? "",
                pruneExampleChat = source.staging.pruneExampleChat,
            };
        }

        if (source.parameters != null)
        {
            localChat.parameters = new Models.Formats.ChatLogs.ChatParameters
            {
                model = source.parameters.model,
                temperature = source.parameters.temperature,
                topP = source.parameters.topP,
                minP = source.parameters.minP,
                topK = source.parameters.topK,
                minPEnabled = source.parameters.minPEnabled,
                repeatLastN = source.parameters.repeatLastN,
                repeatPenalty = source.parameters.repeatPenalty,
                promptTemplate = source.parameters.promptTemplate,
            };
        }

        return localChat;
    }

    /// <summary>
    /// Convert local BackupData.Chat to Ginger.BackupData.Chat.
    /// </summary>
    private static BackupData.Chat FromLocalChat(Models.Formats.ChatLogs.BackupData.Chat source)
    {
        var chat = new BackupData.Chat
        {
            name = source.name,
            creationDate = source.creationDate,
            updateDate = source.updateDate,
            backgroundName = source.backgroundName,
            participants = source.participants,
        };

        if (source.history != null)
        {
            chat.history = new ChatHistory
            {
                name = source.history.name,
                messages = source.history.messages?.Select(m => new ChatHistory.Message
                {
                    speaker = m.speaker,
                    creationDate = m.creationDate,
                    updateDate = m.updateDate,
                    activeSwipe = m.activeSwipe,
                    swipes = m.swipes,
                }).ToArray() ?? Array.Empty<ChatHistory.Message>(),
            };
        }

        if (source.staging != null)
        {
            chat.staging = new Backyard.ChatStaging
            {
                system = source.staging.system ?? "",
                scenario = source.staging.scenario ?? "",
                greeting = Backyard.CharacterMessage.FromString(source.staging.greeting?.text ?? ""),
                example = source.staging.example ?? "",
                grammar = source.staging.grammar ?? "",
                authorNote = source.staging.authorNote ?? "",
                pruneExampleChat = source.staging.pruneExampleChat,
            };
        }

        if (source.parameters != null)
        {
            chat.parameters = new Backyard.ChatParameters
            {
                model = source.parameters.model ?? "",
                temperature = source.parameters.temperature,
                topP = source.parameters.topP,
                minP = source.parameters.minP,
                topK = source.parameters.topK,
                minPEnabled = source.parameters.minPEnabled,
                repeatLastN = source.parameters.repeatLastN,
                repeatPenalty = source.parameters.repeatPenalty,
                promptTemplate = source.parameters.promptTemplate,
            };
        }

        return chat;
    }

    // Convert BackupData.Chat to BackyardChatBackupV2 for writing
    private static Models.Formats.ChatLogs.BackyardChatBackupV2? ConvertToBackyardChatBackup(BackupData.Chat source)
    {
        if (source == null) return null;
        var localChat = ToLocalChat(source);
        return Models.Formats.ChatLogs.BackyardChatBackupV2.FromBackupChat(localChat);
    }

    // Convert BackupData.Chat to GingerChatV2 for writing
    private static Models.Formats.ChatLogs.GingerChatV2? ConvertToGingerChat(BackupData.Chat source)
    {
        if (source == null) return null;
        var localChat = ToLocalChat(source);
        return Models.Formats.ChatLogs.GingerChatV2.FromBackupChat(localChat);
    }

    // Convert BackyardChatBackupV2 to BackupData.Chat for reading
    private static BackupData.Chat ConvertFromBackyardChatBackup(Models.Formats.ChatLogs.BackyardChatBackupV2 source)
    {
        var localChat = source.ToBackupChat();
        return FromLocalChat(localChat);
    }

    // Convert BackyardChatBackupV1 to BackupData.Chat for reading
    private static BackupData.Chat ConvertFromBackyardChatBackupV1(Models.Formats.ChatLogs.BackyardChatBackupV1 source)
    {
        var localChat = source.ToChat();
        return new BackupData.Chat
        {
            name = localChat.name ?? ChatInstance.DefaultName,
            history = new ChatHistory
            {
                name = localChat.name,
                messages = localChat.history?.messages?.Select(m => new ChatHistory.Message
                {
                    speaker = m.speaker,
                    creationDate = m.creationDate,
                    updateDate = m.updateDate,
                    activeSwipe = m.activeSwipe,
                    swipes = m.swipes,
                }).ToArray() ?? Array.Empty<ChatHistory.Message>(),
            },
        };
    }

    // Convert GingerChatV2 to BackupData.Chat for reading
    private static BackupData.Chat ConvertFromGingerChat(Models.Formats.ChatLogs.GingerChatV2 source)
    {
        var localHistory = source.ToHistory();
        var chat = new BackupData.Chat
        {
            name = source.title ?? ChatInstance.DefaultName,
            creationDate = DateTimeExtensions.FromUnixTime(source.createdAt),
            backgroundName = source.backgroundName,
            participants = source.speakers?.Values.ToArray(),
        };

        if (localHistory != null)
        {
            chat.history = new ChatHistory
            {
                name = localHistory.name,
                messages = localHistory.messages?.Select(m => new ChatHistory.Message
                {
                    speaker = m.speaker,
                    creationDate = m.creationDate,
                    updateDate = m.updateDate,
                    activeSwipe = m.activeSwipe,
                    swipes = m.swipes,
                }).ToArray() ?? Array.Empty<ChatHistory.Message>(),
            };
        }

        if (source.staging != null)
        {
            chat.staging = new Backyard.ChatStaging
            {
                system = source.staging.system ?? "",
                scenario = source.staging.scenario ?? "",
                greeting = Backyard.CharacterMessage.FromString(source.staging.greeting ?? ""),
                example = source.staging.example ?? "",
                grammar = source.staging.grammar ?? "",
                authorNote = source.staging.authorNote ?? "",
                pruneExampleChat = source.staging.pruneExampleChat,
            };
        }

        if (source.parameters != null)
        {
            chat.parameters = new Backyard.ChatParameters
            {
                model = source.parameters.model ?? "",
                temperature = source.parameters.temperature,
                topP = source.parameters.topP,
                minP = source.parameters.minP,
                topK = source.parameters.topK,
                minPEnabled = source.parameters.minPEnabled,
                repeatLastN = source.parameters.repeatLastN,
                repeatPenalty = source.parameters.repeatPenalty,
                promptTemplate = source.parameters.promptTemplate,
            };
        }

        return chat;
    }

    // Convert GingerChatV1 to BackupData.Chat for reading
    private static BackupData.Chat ConvertFromGingerChatV1(Models.Formats.ChatLogs.GingerChatV1 source)
    {
        var localHistory = source.ToChat();
        var chat = new BackupData.Chat
        {
            name = source.title ?? ChatInstance.DefaultName,
            creationDate = DateTimeExtensions.FromUnixTime(source.createdAt),
            backgroundName = source.backgroundName,
            participants = source.speakers?.Select(s => s.name).ToArray(),
        };

        if (localHistory != null)
        {
            chat.history = new ChatHistory
            {
                name = localHistory.name,
                messages = localHistory.messages?.Select(m => new ChatHistory.Message
                {
                    speaker = m.speaker,
                    creationDate = m.creationDate,
                    updateDate = m.updateDate,
                    activeSwipe = m.activeSwipe,
                    swipes = m.swipes,
                }).ToArray() ?? Array.Empty<ChatHistory.Message>(),
            };
        }

        if (source.staging != null)
        {
            chat.staging = new Backyard.ChatStaging
            {
                system = source.staging.system ?? "",
                scenario = source.staging.scenario ?? "",
                greeting = Backyard.CharacterMessage.FromString(source.staging.greeting ?? ""),
                example = source.staging.example ?? "",
                grammar = source.staging.grammar ?? "",
                authorNote = source.staging.authorNote ?? "",
                pruneExampleChat = source.staging.pruneExampleChat,
            };
        }

        if (source.parameters != null)
        {
            chat.parameters = new Backyard.ChatParameters
            {
                model = source.parameters.model ?? "",
                temperature = source.parameters.temperature,
                topP = source.parameters.topP,
                minP = source.parameters.minP,
                topK = source.parameters.topK,
                minPEnabled = source.parameters.minPEnabled,
                repeatLastN = source.parameters.repeatLastN,
                repeatPenalty = source.parameters.repeatPenalty,
                promptTemplate = source.parameters.promptTemplate,
            };
        }

        return chat;
    }
}
