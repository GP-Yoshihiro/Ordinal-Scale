"""Import the licensed DemonLord2 Unity package into this machine's ignored assets.

Only Skin1 albedo/normal textures are needed for the iPhone visual prototype.
The original Fab package stays outside the public repository.
"""

from __future__ import annotations

import argparse
import shutil
import tarfile
from pathlib import Path, PurePosixPath

SOURCE_PREFIX = PurePosixPath("Assets/DemonLord2")
ASSET_SUFFIXES = {".fbx", ".prefab", ".mat", ".controller"}


def selected(path: str) -> bool:
    source = PurePosixPath(path)
    if not source.is_relative_to(SOURCE_PREFIX) or ".." in source.parts:
        return False
    if source.suffix.lower() == ".tga":
        return "/Textures/Skin1/" in path and (
            "Albedo" in source.name or "Normal" in source.name
        )
    return source.suffix.lower() in ASSET_SUFFIXES


def import_package(package: Path, destination: Path) -> tuple[int, int]:
    names: dict[str, str] = {}
    with tarfile.open(package, "r|gz") as archive:
        for member in archive:
            parts = member.name.split("/")
            if len(parts) == 2 and parts[1] == "pathname":
                stream = archive.extractfile(member)
                if stream is not None:
                    names[parts[0]] = stream.read().decode("utf-8").strip()

    count = 0
    total = 0
    with tarfile.open(package, "r|gz") as archive:
        for member in archive:
            parts = member.name.split("/")
            if len(parts) != 2 or parts[1] not in {"asset", "asset.meta"}:
                continue
            source = names.get(parts[0], "")
            if not selected(source):
                continue
            relative = PurePosixPath(source).relative_to(SOURCE_PREFIX)
            target = destination.joinpath("DemonLord2", *relative.parts)
            if relative == PurePosixPath("Prefab/BM_DemonLord2.prefab"):
                target = destination.parent / "Resources" / "DemonLord2.prefab"
            if parts[1] == "asset.meta":
                target = target.with_name(target.name + ".meta")
            stream = archive.extractfile(member)
            if stream is None:
                continue
            target.parent.mkdir(parents=True, exist_ok=True)
            temporary = target.with_name(target.name + ".tmp")
            with temporary.open("wb") as output:
                shutil.copyfileobj(stream, output)
            temporary.replace(target)
            if parts[1] == "asset":
                count += 1
                total += member.size
    return count, total


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("package", type=Path, help="Downloaded unitypack.unitypackage")
    parser.add_argument(
        "destination",
        type=Path,
        help="Unity Assets/LocalLicensed/Source directory (ignored by Git)",
    )
    args = parser.parse_args()
    count, size = import_package(args.package, args.destination)
    print(f"Imported {count} assets ({size / 1048576:.1f} MiB) to {args.destination}")
