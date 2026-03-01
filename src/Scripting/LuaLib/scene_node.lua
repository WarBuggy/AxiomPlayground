-- ============================================
-- Engine Utility — Node
-- Thin Lua wrapper over the C# scene tree API.
-- Available as a global to all mods.
-- ============================================

Node = {}
Node.__index = Node

function Node.new(opts)
    opts = opts or {}
    local self = setmetatable({}, Node)
    self.name        = opts.name or "unnamed"
    self._onEnter    = opts.onEnter
    self._onExit     = opts.onExit
    self._onUpdate   = opts.onUpdate
    self._onDraw     = opts.onDraw
    self.active      = opts.active ~= false
    self.shared      = opts.shared
    self.blocksInput = opts.blocksInput or false
    self._children   = {}
    self._childMap   = {}
    self._parent     = nil
    self._live       = false
    self._registered = false
    self._pendingChildren = {}
    return self
end

function Node:_engineOpts()
    local node = self
    return {
        name = self.name,
        active = self.active,
        blocksInput = self.blocksInput or false,
        shared = self.shared,
        onEnter = self._onEnter and function(shared)
            node._onEnter(node, shared)
        end or nil,
        onExit = self._onExit and function(shared)
            node._onExit(node, shared)
        end or nil,
        onUpdate = self._onUpdate and function(dt, totalTime, shared)
            node._onUpdate(node, dt, totalTime, shared)
        end or nil,
        onDraw = self._onDraw and function(shared)
            node._onDraw(node, shared)
        end or nil,
    }
end

function Node:addChild(child)
    if self._childMap[child.name] then return self end
    child._parent = self
    self._children[#self._children + 1] = child
    self._childMap[child.name] = child

    if self._registered then
        Scene.AddChild(self.name, child:_engineOpts())
    else
        self._pendingChildren[#self._pendingChildren + 1] = child
    end
    return self
end

function Node:removeChild(name)
    local child = self._childMap[name]
    if not child then return self end
    child._parent = nil
    self._childMap[name] = nil
    for i = #self._children, 1, -1 do
        if self._children[i] == child then
            table.remove(self._children, i)
            break
        end
    end
    if self._registered then
        Scene.RemoveChild(self.name, name)
    end
    return self
end

function Node:getChild(name)
    return self._childMap[name]
end

function Node:find(name)
    if self._childMap[name] then return self._childMap[name] end
    for _, child in ipairs(self._children) do
        local found = child:find(name)
        if found then return found end
    end
    return nil
end

function Node:setActive(bool)
    if self.active == bool then return end
    self.active = bool
    if self._registered or (self._parent and self._parent._registered) then
        Scene.SetNodeActive(self.name, bool)
    end
end

function Node:getShared()
    if self.shared then return self.shared end
    if self._parent then return self._parent:getShared() end
    return nil
end

function Node:registerAsScene(sceneName)
    Scene.RegisterTree(sceneName, self:_engineOpts())
    self._registered = true

    for _, child in ipairs(self._pendingChildren) do
        Scene.AddChild(self.name, child:_engineOpts())
        child._registered = true
        child:_flushPending()
    end
    self._pendingChildren = {}
end

function Node:_flushPending()
    for _, child in ipairs(self._pendingChildren) do
        Scene.AddChild(self.name, child:_engineOpts())
        child._registered = true
        child:_flushPending()
    end
    self._pendingChildren = {}
end
