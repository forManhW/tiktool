import asyncio
import hashlib
import json
import sys


def emit(kind, **data):
    print(json.dumps({"type": kind, **data}, ensure_ascii=False), flush=True)


def catalog(payload):
    result, seen = [], set()
    def walk(value):
        if isinstance(value, dict):
            gift_id, name = value.get("id") or value.get("gift_id"), value.get("name") or value.get("gift_name")
            if gift_id is not None and name and str(gift_id) not in seen:
                seen.add(str(gift_id))
                image = value.get("image") or {}
                urls = image.get("url_list", []) if isinstance(image, dict) else []
                result.append({"id": str(gift_id), "name": str(name), "diamondCount": int(value.get("diamond_count") or 0), "imageUrl": str(urls[0]) if urls else ""})
            for child in value.values(): walk(child)
        elif isinstance(value, (list, tuple)):
            for child in value: walk(child)
    walk(payload)
    return result


async def run(username):
    from TikTokLive import TikTokLiveClient
    from TikTokLive.events import ConnectEvent, GiftEvent
    client = TikTokLiveClient(unique_id=username)
    combo = {}

    async def connected(event):
        emit("connected", username=getattr(event, "unique_id", username), roomId=str(getattr(client, "room_id", "")))
        emit("catalog", gifts=catalog(getattr(client, "gift_info", None) or {}))

    async def gifted(event):
        gift = event.gift
        if not gift: return
        group = str(getattr(event, "group_id", "") or "")
        total = max(1, int(getattr(event, "repeat_count", 1) or 1))
        key = f"{getattr(client, 'room_id', '')}:{getattr(event.user, 'unique_id', '')}:{group or getattr(gift, 'id', '')}"
        previous = combo.get(key, 0)
        delta = max(0, total - previous) if group and group != "0" else total
        if group and group != "0": combo[key] = 0 if getattr(event, "repeat_end", False) else total
        if not delta: return
        message_id = str(getattr(event.common, "msg_id", "") or "") or hashlib.sha256(bytes(event)).hexdigest()
        image = getattr(gift, "image", None)
        urls = getattr(image, "url_list", []) if image else []
        emit("gift", eventId=f"tiktok:{getattr(client, 'room_id', '')}:{message_id}:{total}", roomId=str(getattr(client, "room_id", "")), giftId=str(gift.id), giftName=str(gift.name), count=delta, user={"id": str(getattr(event.user, "unique_id", "")), "username": str(getattr(event.user, "nickname", "") or getattr(event.user, "unique_id", ""))}, diamondCount=int(getattr(gift, "diamond_count", 0) or 0), imageUrl=str(urls[0]) if urls else "")

    client.add_listener(ConnectEvent, connected)
    client.add_listener(GiftEvent, gifted)
    try:
        await client.connect(fetch_gift_info=True)
    except Exception as exc:
        emit("error", message=f"{type(exc).__name__}: {exc}")
    finally:
        try: await client.disconnect(close_client=True)
        except Exception: pass
        emit("disconnected")


if __name__ == "__main__":
    try: asyncio.run(run(sys.argv[1].lstrip("@")))
    except Exception as exc: emit("error", message=f"{type(exc).__name__}: {exc}")
