using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

UndertaleData DonorData;

void DeepCopyData(Manifest mod) {
    var dataPath = Path.Join(mod.Directory.FullName, mod.DataPath);

    logger.Info(mod.Name, $"Loading donor data file ...");

    using (var stream = new FileStream(dataPath, FileMode.Open, FileAccess.Read)) {
        DonorData = UndertaleIO.Read(stream, (warning, _) =>
            logger.Warn(mod.Name, $"A warning occurred while trying to load {dataPath}: {warning}"));
    }

    DonorData.BuiltinList = new(DonorData);
    GameSpecificResolver.Initialize(DonorData);

    if (mod.GameObjects.Count > 0) {
        logger.Info(mod.Name, $"Deep copying {mod.GameObjects.Count} game {(mod.GameObjects.Count > 1 ? "objects" : "object")} ...");

        foreach (var gameObjectName in mod.GameObjects) {
            DeepCopyUndertaleGameObject(DonorData.GameObjects.ByName(gameObjectName));
        }
    }

    if (mod.Rooms.Count > 0) {
        logger.Info(mod.Name, $"Deep copying {mod.Rooms.Count} {(mod.Rooms.Count > 1 ? "rooms" : "room")} ...");

        foreach (var roomName in mod.Rooms) {
            DeepCopyUndertaleRoom(DonorData.Rooms.ByName(roomName));
        }
    }
}

UndertaleGameObject DeepCopyUndertaleGameObject(UndertaleGameObject gameObject) {
    var result = Data.GameObjects.ByName(gameObject.Name.Content);
    if (result is null) {
        result = new();
        Data.GameObjects.Add(result);
    }

    foreach (var property in GetSettableProperties<UndertaleGameObject>()) {
        property.SetValue(result, property.GetValue(gameObject) switch {
            UndertaleString name => Data.Strings.MakeString(name.Content),

            UndertaleSprite sprite => Data.Sprites.ByName(sprite.Name.Content),

            UndertaleGameObject parent => Data.GameObjects.ByName(parent.Name.Content),

            List<UndertaleGameObject.UndertalePhysicsVertex> physicsVertices =>
                physicsVertices.Select(vertex => new UndertaleGameObject.UndertalePhysicsVertex()
                    {
                        X = vertex.X,
                        Y = vertex.Y
                    }).ToList(),

            UndertalePointerList<UndertalePointerList<UndertaleGameObject.Event>> events =>
                DeepCopyUndertalePointerList(events, subEvents =>
                    DeepCopyUndertalePointerList(subEvents, DeepCopyEvent)),

            var value when property.PropertyType.IsPrimitive
                || value is CollisionShapeFlags
                || value is null
                    => value,
        });
    }

    return result;
}

UndertaleGameObject.Event DeepCopyEvent(UndertaleGameObject.Event source) {
    var result = new UndertaleGameObject.Event();

    foreach (var property in GetSettableProperties<UndertaleGameObject.Event>()) {
        property.SetValue(result, property.GetValue(source) switch {
            UndertalePointerList<UndertaleGameObject.EventAction> actions =>
                DeepCopyUndertalePointerList(actions, DeepCopyEventAction),

            var value when property.PropertyType.IsPrimitive
                || value is EventSubtypeKey
                || value is EventSubtypeStep
                || value is EventSubtypeMouse
                || value is EventSubtypeOther
                || value is EventSubtypeDraw
                || value is EventSubtypeGesture
                || value is null
                    => value,
        });
    }

    return result;
}

UndertaleGameObject.EventAction DeepCopyEventAction(UndertaleGameObject.EventAction source) {
    var result = new UndertaleGameObject.EventAction();

    foreach (var property in GetSettableProperties<UndertaleGameObject.EventAction>()) {
        property.SetValue(result, property.GetValue(source) switch {
            UndertaleString name => Data.Strings.MakeString(name.Content),

            UndertaleCode code => DeepCopyUndertaleCode(code),

            var value when property.PropertyType.IsPrimitive
                || value is null
                    => value,

            _ => throw new Exception($"{property.Name} unhandled by switch"),
        });
    }

    return result;
}

UndertaleCode DeepCopyUndertaleCode(UndertaleCode source, bool isChildEntry = false) {
    var globalDecompileContext = new GlobalDecompileContext(DonorData);
    if (!isChildEntry) {
        var codeToCopy = "";

        try {
            codeToCopy = new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, source, Data.ToolInfo.DecompilerSettings)
                .DecompileToString();
        } catch (Exception e) {
            codeToCopy = "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/";
        }

        try {
            var importGroup = new UndertaleModLib.Compiler.CodeImportGroup(Data);
            importGroup.QueueReplace(source.Name.Content, codeToCopy);
            importGroup.Import();
        } catch (Exception e) {
            ScriptError("Uh oh, " + source.Name.Content + " has an error: " + e.Message);
        }
    }

    var result = Data.Code.ByName(source.Name.Content);

    foreach (var property in GetSettableProperties<UndertaleCode>()) {
        property.SetValue(result, property.GetValue(source) switch {
            UndertaleString undertaleString => Data.Strings.MakeString(undertaleString.Content),

            UndertaleCode parentEntry => parentEntry,

            List<UndertaleCode> childEntries => childEntries.Select(entry => DeepCopyUndertaleCode(entry, true)).ToList(),

            var value when property.PropertyType.IsPrimitive => value,

            null => null,
        });
    }

    if (!isChildEntry && Data.CodeLocals is not null) {
        var locals = Data.CodeLocals.ByName(source.Name.Content);
        if (locals is null) {
            locals = new();
            locals.Name = Data.Strings.MakeString(source.Name.Content);
        }

        locals.Locals.Clear();

        foreach (var local in DonorData.CodeLocals.ByName(source.Name.Content).Locals) {
            locals.Locals.Add(new() {
                Name = Data.Strings.MakeString(local.Name.Content),
                Index = local.Index,
            });
        }

        result.LocalsCount = (uint)locals.Locals.Count;
    }

    return result;
}

UndertaleRoom DeepCopyUndertaleRoom(UndertaleRoom room) {
    var result = Data.Rooms.ByName(room.Name.Content);
    if (result is null) {
        result = new();
        Data.Rooms.Add(result);
    }

    foreach (var property in GetSettableProperties<UndertaleRoom>()) {
        property.SetValue(result, property.GetValue(room) switch {
            UndertaleString undertaleString => Data.Strings.MakeString(undertaleString.Content),

            UndertaleCode creationCode => DeepCopyUndertaleCode(creationCode),

            UndertaleRoom.InstanceIDList instanceIDList => new UndertaleRoom.InstanceIDList
                {
                    InstanceIDs = DeepCopyUndertaleList(instanceIDList.InstanceIDs, (int instanceID) => instanceID)
                },
            
            UndertalePointerList<UndertaleRoom.Background> backgrounds =>
                DeepCopyUndertalePointerList(backgrounds, DeepCopyUndertaleRoomBackground),

            UndertalePointerList<UndertaleRoom.View> views =>
                DeepCopyUndertalePointerList(views, DeepCopyUndertaleRoomView),

            UndertalePointerList<UndertaleRoom.GameObject> gameObjects =>
                DeepCopyUndertalePointerList(gameObjects, DeepCopyUndertaleRoomGameObject),

            UndertalePointerList<UndertaleRoom.Tile> tiles =>
                DeepCopyUndertalePointerList(tiles, DeepCopyUndertaleRoomTile),

            UndertalePointerList<UndertaleRoom.Layer> layers =>
                DeepCopyUndertalePointerList(layers, DeepCopyUndertaleRoomLayer),

            UndertaleSimpleList<UndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>> sequences =>
                DeepCopyUndertaleList(sequences, DeepCopyUndertaleResourceById<UndertaleSequence, UndertaleChunkSEQN>(DeepCopyUndertaleSequence)),

            var value when property.PropertyType.IsPrimitive || value is UndertaleRoom.RoomEntryFlags => value,

            _ => property.GetValue(result)
        });
    }

    return result;
}

UndertaleRoom.Background DeepCopyUndertaleRoomBackground(UndertaleRoom.Background background) {
    var result = new UndertaleRoom.Background();

    foreach (var property in GetSettableProperties<UndertaleRoom.Background>()) {
        property.SetValue(result, property.GetValue(background) switch {
            UndertaleRoom parentRoom => DeepCopyUndertaleRoom(parentRoom),

            UndertaleBackground backgroundDefinition => DeepCopyUndertaleBackground(backgroundDefinition),

            var value when property.PropertyType.IsPrimitive => value,
        });
    }

    return result;
}

UndertaleBackground DeepCopyUndertaleBackground(UndertaleBackground background) {
    var result = new UndertaleBackground();

    foreach (var property in GetSettableProperties<UndertaleBackground>()) {
        property.SetValue(result, property.GetValue(background) switch {
            UndertaleString name => Data.Strings.MakeString(name.Content),

            // TODO: maybe could be handled by the sprite importer
            UndertaleTexturePageItem texture => null,

            List<UndertaleBackground.TileID> gms2TileIds =>
                gms2TileIds.Select(tileId => new UndertaleBackground.TileID() { ID = tileId.ID }).ToList(),

            var value when property.PropertyType.IsPrimitive => value,
        });
    }

    return result;
}

UndertaleRoom.View DeepCopyUndertaleRoomView(UndertaleRoom.View view) {
    var result = new UndertaleRoom.View();

    foreach (var property in GetSettableProperties<UndertaleRoom.View>()) {
        property.SetValue(result, property.GetValue(view) switch {
            UndertaleGameObject gameObject => Data.GameObjects.ByName(gameObject.Name.Content),

            var value when property.PropertyType.IsPrimitive => value,

            _ => property.GetValue(result),
        });
    }

    return result;
}

UndertaleRoom.GameObject DeepCopyUndertaleRoomGameObject(UndertaleRoom.GameObject gameObject) {
    var result = new UndertaleRoom.GameObject();

    foreach (var property in GetSettableProperties<UndertaleRoom.GameObject>()) {
        property.SetValue(result, property.GetValue(gameObject) switch {
            UndertaleGameObject objectDefinition => Data.GameObjects.ByName(objectDefinition.Name.Content),

            UndertaleCode code => DeepCopyUndertaleCode(code),

            var value when property.PropertyType.IsPrimitive => value,

            // TODO: figure out which field is null
            _ => property.GetValue(result),
        });
    }

    return result;
}

UndertaleRoom.Tile DeepCopyUndertaleRoomTile(UndertaleRoom.Tile tile) {
    var result = new UndertaleRoom.Tile();

    result.spriteMode = tile.spriteMode;

    foreach (var property in GetSettableProperties<UndertaleRoom.Tile>()) {
        property.SetValue(result, property.GetValue(tile) switch {
            UndertaleBackground backgroundDefinition => DeepCopyUndertaleBackground(backgroundDefinition),

            UndertaleSprite sprite => Data.Sprites.ByName(sprite.Name.Content),

            // TODO: same as above
            UndertaleTexturePageItem texture => null,

            var value when property.PropertyType.IsPrimitive || value is UndertaleNamedResource => value,
        });
    }

    return result;
}

UndertaleRoom.Layer DeepCopyUndertaleRoomLayer(UndertaleRoom.Layer layer) {
    var result = new UndertaleRoom.Layer();

    foreach (var property in GetSettableProperties<UndertaleRoom.Layer>()) {
        property.SetValue(result, property.GetValue(layer) switch {
            UndertaleRoom parentRoom => Data.Rooms.ByName(parentRoom.Name.Content),

            UndertaleString undertaleString => Data.Strings.MakeString(undertaleString.Content),

            UndertaleRoom.Layer.LayerData layerData => layerData switch
                {
                    UndertaleRoom.Layer.LayerInstancesData instancesData => DeepCopyLayerInstancesData(instancesData),
                    UndertaleRoom.Layer.LayerTilesData tilesData => DeepCopyLayerTilesData(tilesData),
                    UndertaleRoom.Layer.LayerBackgroundData backgroundData => DeepCopyLayerBackgroundData(backgroundData),
                    UndertaleRoom.Layer.LayerAssetsData assetsData => DeepCopyLayerAssetsData(assetsData),
                    UndertaleRoom.Layer.LayerEffectData effectData => DeepCopyLayerEffectData(effectData),
                },

            var value when property.PropertyType.IsPrimitive || value is UndertaleRoom.LayerType => value,

            // TODO: figure out which field is null
            _ => property.GetValue(result),
        });
    }

    return result;
}

// TODO
UndertaleRoom.Layer.LayerInstancesData DeepCopyLayerInstancesData(UndertaleRoom.Layer.LayerInstancesData instancesData) {
    var result = new UndertaleRoom.Layer.LayerInstancesData();

    result.Instances = new(instancesData.Instances.Select(DeepCopyUndertaleRoomGameObject));

    return result;
}

// TODO
UndertaleRoom.Layer.LayerTilesData DeepCopyLayerTilesData(UndertaleRoom.Layer.LayerTilesData tilesData) {
    var result = new UndertaleRoom.Layer.LayerTilesData();

    return result;
}

// TODO
UndertaleRoom.Layer.LayerBackgroundData DeepCopyLayerBackgroundData(UndertaleRoom.Layer.LayerBackgroundData backgroundData) {
    var result = new UndertaleRoom.Layer.LayerBackgroundData();

    foreach (var property in GetSettableProperties<UndertaleRoom.Layer.LayerBackgroundData>()) {
        property.SetValue(result, property.GetValue(backgroundData) switch {
            UndertaleSprite sprite => Data.Sprites.ByName(sprite.Name.Content),

            var value when property.PropertyType.IsPrimitive || value is AnimationSpeedType => value,

            _ => property.GetValue(result),
        });
    }

    return result;
}

// TODO
UndertaleRoom.Layer.LayerAssetsData DeepCopyLayerAssetsData(UndertaleRoom.Layer.LayerAssetsData assetsData) {
    var result = new UndertaleRoom.Layer.LayerAssetsData();

    return result;
}

// TODO
UndertaleRoom.Layer.LayerEffectData DeepCopyLayerEffectData(UndertaleRoom.Layer.LayerEffectData effectData) {
    var result = new UndertaleRoom.Layer.LayerEffectData();

    return result;
}

// TODO
UndertaleSequence DeepCopyUndertaleSequence(UndertaleSequence sequence) {
    var result = new UndertaleSequence();

    foreach (var property in GetSettableProperties<UndertaleSequence>()) {
        property.SetValue(result, property.GetValue(sequence) switch {
            _ => property.GetValue(result),
        });
    }

    return result;
}

static Func<UndertaleResourceById<T, ChunkT>, UndertaleResourceById<T, ChunkT>> DeepCopyUndertaleResourceById<T, ChunkT>(Func<T, T> transform)
        where T : UndertaleResource, new()
        where ChunkT : UndertaleListChunk<T>
    => undertaleResourceById => new(transform(undertaleResourceById.Resource), undertaleResourceById.CachedId);

static UndertalePointerList<T> DeepCopyUndertalePointerList<T>(UndertalePointerList<T> source, Func<T, T> transform)
        where T : UndertaleObject, new()
    => DeepCopyUndertaleList(source, transform);

static TSource DeepCopyUndertaleList<TSource, T>(TSource source, Func<T, T> transform)
        where TSource : UndertaleObservableList<T>, new() {
    var list = new TSource();

    foreach (var item in source) {
        list.Add(transform(item));
    }

    return list;
}

// static UndertalePointerList<T> DeepCopyUndertalePointerList<T>(UndertalePointerList<T> source, Func<T, T> transform)
//     where T : UndertaleObject, new()
// {
//     var list = new UndertalePointerList<T>();
//
//     foreach (var item in source)
//         list.Add(transform(item));
//
//     return list;
// }

static IEnumerable<PropertyInfo> GetSettableProperties<T>() =>
    typeof(T).GetProperties().Where(property => property.SetMethod?.IsPublic ?? false);

// vim:ts=4 sts=4 sw=4
