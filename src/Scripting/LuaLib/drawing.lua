-- ============================================
-- Engine Utility — Drawing Wrappers
-- Batches Drawing.Text calls into a stride-5
-- buffer, flushed in one C# crossing via
-- Drawing.ResetLayer (end of frame).
-- API unchanged: Drawing.Text(text, x, y, sz, color)
-- ============================================

if not Drawing then return end

-- Text batch buffer (stride-5: text, x, y, size, packedColor)
local _tbuf = {}
local _tn = 0

local _origResetLayer = Drawing.ResetLayer

-- Replace Drawing.Text with Lua-side accumulator
Drawing.Text = function(text, x, y, size, color)
    -- Convert table colors to packed int at accumulation time
    if type(color) == "table" then
        color = Color.New(color[1] or 255, color[2] or 255, color[3] or 255, color[4] or 255)
    end
    _tbuf[_tn+1] = text
    _tbuf[_tn+2] = x or 0
    _tbuf[_tn+3] = y or 0
    _tbuf[_tn+4] = size or 16
    _tbuf[_tn+5] = color or 0
    _tn = _tn + 5
end

-- Hook ResetLayer to flush text buffer (called at end of each frame)
Drawing.ResetLayer = function()
    if _tn > 0 then
        Drawing.FlushText(_tbuf, _tn / 5)
        _tn = 0
    end
    _origResetLayer()
end
