-- ============================================
-- Engine Utility — Input Bridge Wrappers
-- Transparently replaces high-frequency C#
-- bindings with pure Lua reads from a cached
-- table. Keyboard keys auto-register on first
-- use — no manual setup needed from mods.
-- API unchanged: Input.MouseX(), Screen.Width()
-- ============================================

local _F = Input._cache
if not _F then return end -- InputBridge not registered (headless?)

-- ========== Save original C# delegates (fallback for first key access) ==========
local _origIsKeyDown    = Input.IsKeyDown
local _origIsKeyPressed = Input.IsKeyPressed
local _origIsKeyReleased = Input.IsKeyReleased

-- Track which keys have been registered (avoid repeated C# registration calls)
local _registered = {} -- ["down:a"] = true

-- ========== Mouse position (zero crossing) ==========
Input.MouseX = function() return _F.mouseX or 0 end
Input.MouseY = function() return _F.mouseY or 0 end

-- ========== Mouse buttons (zero crossing) ==========
-- Lookup tables for button → cache key prefix (avoids repeated string comparisons)
local _mbDown = {
    left = "leftDown", lmb = "leftDown", leftmouse = "leftDown",
    right = "rightDown", rmb = "rightDown", rightmouse = "rightDown",
    middle = "middleDown", mmb = "middleDown", middlemouse = "middleDown",
    x1 = "x1Down", mouse4 = "x1Down", back = "x1Down", xbutton1 = "x1Down",
    x2 = "x2Down", mouse5 = "x2Down", forward = "x2Down", xbutton2 = "x2Down",
}
local _mbPressed = {
    left = "leftPressed", lmb = "leftPressed", leftmouse = "leftPressed",
    right = "rightPressed", rmb = "rightPressed", rightmouse = "rightPressed",
    middle = "middlePressed", mmb = "middlePressed", middlemouse = "middlePressed",
    x1 = "x1Pressed", mouse4 = "x1Pressed", back = "x1Pressed", xbutton1 = "x1Pressed",
    x2 = "x2Pressed", mouse5 = "x2Pressed", forward = "x2Pressed", xbutton2 = "x2Pressed",
}
local _mbReleased = {
    left = "leftReleased", lmb = "leftReleased", leftmouse = "leftReleased",
    right = "rightReleased", rmb = "rightReleased", rightmouse = "rightReleased",
    middle = "middleReleased", mmb = "middleReleased", middlemouse = "middleReleased",
    x1 = "x1Released", mouse4 = "x1Released", back = "x1Released", xbutton1 = "x1Released",
    x2 = "x2Released", mouse5 = "x2Released", forward = "x2Released", xbutton2 = "x2Released",
}

Input.IsMouseDown = function(btn)
    local k = _mbDown[btn]; return k and (_F[k] or false) or false
end
Input.IsMousePressed = function(btn)
    local k = _mbPressed[btn]; return k and (_F[k] or false) or false
end
Input.IsMouseReleased = function(btn)
    local k = _mbReleased[btn]; return k and (_F[k] or false) or false
end

-- ========== Scroll & number key (zero crossing) ==========
Input.ScrollDelta = function() return _F.scrollDelta or 0 end
Input.GetNumberKeyPressed = function() return _F.numberKey or -1 end

-- ========== Screen (zero crossing) ==========
Screen.Width = function() return _F.screenW or 0 end
Screen.Height = function() return _F.screenH or 0 end

-- ========== Keyboard (auto-register on first use) ==========
-- First call: registers the key with C# via Frame._WatchKey, falls back to C# delegate.
-- All subsequent frames: C# pushes the value into Frame, read is pure Lua (zero crossing).

Input.IsKeyDown = function(key)
    local v = _F["kd_" .. key]
    if v ~= nil then return v end
    -- First access — register and fall back to C#
    local rk = "d:" .. key
    if not _registered[rk] then
        _registered[rk] = true
        _F._WatchKey(key, "down")
    end
    return _origIsKeyDown(key)
end

Input.IsKeyPressed = function(key)
    local v = _F["kp_" .. key]
    if v ~= nil then return v end
    local rk = "p:" .. key
    if not _registered[rk] then
        _registered[rk] = true
        _F._WatchKey(key, "pressed")
    end
    return _origIsKeyPressed(key)
end

Input.IsKeyReleased = function(key)
    local v = _F["kr_" .. key]
    if v ~= nil then return v end
    local rk = "r:" .. key
    if not _registered[rk] then
        _registered[rk] = true
        _F._WatchKey(key, "released")
    end
    return _origIsKeyReleased(key)
end
