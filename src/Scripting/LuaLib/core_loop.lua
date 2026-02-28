-- ============================================
-- Engine Utility — Core Loop
-- Auto-ticks engine utilities each frame.
-- Engine calls __engineCoreUpdate(dt, totalTime) every frame.
-- Utilities register via __engineCoreRegister(fn).
-- ============================================

local _updaters = {}

function __engineCoreRegister(fn)
    _updaters[#_updaters + 1] = fn
end

function __engineCoreUpdate(dt, totalTime)
    for _, fn in ipairs(_updaters) do
        fn(dt, totalTime)
    end
end
