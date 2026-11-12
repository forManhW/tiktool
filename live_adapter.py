import asyncio
import hashlib
import json
import sys


def emit(kind, **data):
    # ensure_ascii=True: tiếng Việt / emoji được mã hóa \uXXXX nên stdout luôn chỉ có ASCII.
    # Trước đây ensure_ascii=False làm Windows (stdout cp1252) báo UnicodeEncodeError và BỎ MẤT
    # sự kiện của người xem có tên / comment tiếng Việt hoặc emoji.
    print(json.dumps({"type": kind, **data}, ensure_ascii=True), flush=True)


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


def viewer(user):
    if not user: return {"id": "", "username": "", "nickname": ""}
    unique_id = str(getattr(user, "unique_id", "") or "")
    avatar = getattr(user, "avatar_thumb", None)
    urls = list(getattr(avatar, "m_urls", None) or getattr(avatar, "url_list", None) or []) if avatar else []
    return {"id": unique_id, "username": unique_id, "nickname": str(getattr(user, "nickname", "") or unique_id), "avatarUrl": str(urls[0]) if urls else ""}


def message_id(event):
    msg = str(getattr(getattr(event, "common", None), "msg_id", "") or "")
    return msg or hashlib.sha256(bytes(event)).hexdigest()


async def run(username):
    from TikTokLive import TikTokLiveClient
    from TikTokLive.events import ConnectEvent, GiftEvent, LikeEvent, CommentEvent, FollowEvent, ShareEvent
    client = TikTokLiveClient(unique_id=username)
    combo = {}
    room = lambda: str(getattr(client, "room_id", "") or "")

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
        image = getattr(gift, "image", None)
        urls = getattr(image, "url_list", []) if image else []
        emit("gift", eventId=f"tiktok:{room()}:{message_id(event)}:{total}", roomId=str(getattr(client, "room_id", "")), giftId=str(gift.id), giftName=str(gift.name), count=delta, user=viewer(event.user), diamondCount=int(getattr(gift, "diamond_count", 0) or 0), imageUrl=str(urls[0]) if urls else "")

    async def liked(event):
        count = max(0, int(getattr(event, "count", 0) or 0))
        if count: emit("like", eventId=f"tiktok:{room()}:{message_id(event)}", roomId=room(), count=count, total=int(getattr(event, "total", 0) or 0), user=viewer(event.user))

    async def commented(event):
        text = str(getattr(event, "comment", "") or getattr(event, "content", "") or "")
        if text: emit("comment", eventId=f"tiktok:{room()}:{message_id(event)}", roomId=room(), text=text[:500], user=viewer(event.user))

    async def followed(event):
        emit("follow", eventId=f"tiktok:{room()}:{message_id(event)}", roomId=room(), user=viewer(event.user))

    async def shared(event):
        emit("share", eventId=f"tiktok:{room()}:{message_id(event)}", roomId=room(), count=1, user=viewer(event.user))

    client.add_listener(ConnectEvent, connected)
    client.add_listener(GiftEvent, gifted)
    client.add_listener(LikeEvent, liked)
    client.add_listener(CommentEvent, commented)
    client.add_listener(FollowEvent, followed)
    client.add_listener(ShareEvent, shared)
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
