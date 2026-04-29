using System.Collections.Generic;

namespace Quraan.Application.Audio;

public sealed record AudioRecitationDto(
    byte SurahId,
    ReciterInfoDto Reciter,
    string AudioUrl,
    bool HasTimings,
    IReadOnlyList<AyahTimingDto> AyahTimings);
