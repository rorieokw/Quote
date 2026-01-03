// Photo Markup Canvas - Fabric.js wrapper for annotation tools
window.photoMarkup = {
    canvas: null,
    currentTool: 'pen',
    currentColor: '#ef4444',
    currentStrokeWidth: 3,
    history: [],
    historyIndex: -1,
    isDrawing: false,
    startPoint: null,
    activeShape: null,

    // Initialize canvas with background image
    initialize: function (canvasId, imageUrl, width, height) {
        return new Promise((resolve, reject) => {
            try {
                // Create Fabric canvas
                this.canvas = new fabric.Canvas(canvasId, {
                    width: width,
                    height: height,
                    isDrawingMode: true,
                    selection: false
                });

                // Set up free drawing brush
                this.canvas.freeDrawingBrush.color = this.currentColor;
                this.canvas.freeDrawingBrush.width = this.currentStrokeWidth;

                // Load background image
                if (imageUrl) {
                    fabric.Image.fromURL(imageUrl, (img) => {
                        // Scale image to fit canvas while maintaining aspect ratio
                        const scale = Math.min(width / img.width, height / img.height);
                        img.scale(scale);
                        img.set({
                            originX: 'center',
                            originY: 'center',
                            left: width / 2,
                            top: height / 2
                        });
                        this.canvas.setBackgroundImage(img, this.canvas.renderAll.bind(this.canvas));
                        this.saveState();
                        resolve(true);
                    }, { crossOrigin: 'anonymous' });
                } else {
                    this.saveState();
                    resolve(true);
                }

                // Set up event handlers for shapes
                this.setupShapeDrawing();

            } catch (error) {
                console.error('Error initializing photo markup:', error);
                reject(error);
            }
        });
    },

    // Set up mouse events for shape drawing
    setupShapeDrawing: function () {
        this.canvas.on('mouse:down', (o) => {
            if (this.currentTool === 'pen' || this.currentTool === 'eraser') return;

            this.isDrawing = true;
            const pointer = this.canvas.getPointer(o.e);
            this.startPoint = { x: pointer.x, y: pointer.y };

            if (this.currentTool === 'rectangle') {
                this.activeShape = new fabric.Rect({
                    left: pointer.x,
                    top: pointer.y,
                    width: 0,
                    height: 0,
                    fill: 'transparent',
                    stroke: this.currentColor,
                    strokeWidth: this.currentStrokeWidth,
                    selectable: false,
                    evented: false
                });
                this.canvas.add(this.activeShape);
            } else if (this.currentTool === 'circle') {
                this.activeShape = new fabric.Circle({
                    left: pointer.x,
                    top: pointer.y,
                    radius: 0,
                    fill: 'transparent',
                    stroke: this.currentColor,
                    strokeWidth: this.currentStrokeWidth,
                    selectable: false,
                    evented: false
                });
                this.canvas.add(this.activeShape);
            } else if (this.currentTool === 'arrow' || this.currentTool === 'line') {
                this.activeShape = new fabric.Line([pointer.x, pointer.y, pointer.x, pointer.y], {
                    stroke: this.currentColor,
                    strokeWidth: this.currentStrokeWidth,
                    selectable: false,
                    evented: false
                });
                this.canvas.add(this.activeShape);
            }
        });

        this.canvas.on('mouse:move', (o) => {
            if (!this.isDrawing || !this.activeShape) return;

            const pointer = this.canvas.getPointer(o.e);

            if (this.currentTool === 'rectangle') {
                const width = pointer.x - this.startPoint.x;
                const height = pointer.y - this.startPoint.y;
                this.activeShape.set({
                    width: Math.abs(width),
                    height: Math.abs(height),
                    left: width > 0 ? this.startPoint.x : pointer.x,
                    top: height > 0 ? this.startPoint.y : pointer.y
                });
            } else if (this.currentTool === 'circle') {
                const radius = Math.sqrt(
                    Math.pow(pointer.x - this.startPoint.x, 2) +
                    Math.pow(pointer.y - this.startPoint.y, 2)
                ) / 2;
                this.activeShape.set({ radius: radius });
            } else if (this.currentTool === 'arrow' || this.currentTool === 'line') {
                this.activeShape.set({ x2: pointer.x, y2: pointer.y });
            }

            this.canvas.renderAll();
        });

        this.canvas.on('mouse:up', (o) => {
            if (!this.isDrawing) return;

            // Add arrowhead for arrow tool
            if (this.currentTool === 'arrow' && this.activeShape) {
                const line = this.activeShape;
                const angle = Math.atan2(line.y2 - line.y1, line.x2 - line.x1);
                const headLength = 15;

                const arrowHead = new fabric.Triangle({
                    left: line.x2,
                    top: line.y2,
                    width: headLength,
                    height: headLength,
                    fill: this.currentColor,
                    angle: (angle * 180 / Math.PI) + 90,
                    originX: 'center',
                    originY: 'center',
                    selectable: false,
                    evented: false
                });
                this.canvas.add(arrowHead);
            }

            this.isDrawing = false;
            this.activeShape = null;
            this.saveState();
        });

        // Save state after free drawing
        this.canvas.on('path:created', () => {
            this.saveState();
        });
    },

    // Set current tool
    setTool: function (tool) {
        this.currentTool = tool;

        if (tool === 'pen') {
            this.canvas.isDrawingMode = true;
            this.canvas.freeDrawingBrush.color = this.currentColor;
            this.canvas.freeDrawingBrush.width = this.currentStrokeWidth;
        } else if (tool === 'eraser') {
            this.canvas.isDrawingMode = true;
            this.canvas.freeDrawingBrush.color = '#ffffff';
            this.canvas.freeDrawingBrush.width = this.currentStrokeWidth * 3;
        } else {
            this.canvas.isDrawingMode = false;
        }
    },

    // Set drawing color
    setColor: function (color) {
        this.currentColor = color;
        if (this.canvas.isDrawingMode && this.currentTool !== 'eraser') {
            this.canvas.freeDrawingBrush.color = color;
        }
    },

    // Set stroke width
    setStrokeWidth: function (width) {
        this.currentStrokeWidth = width;
        if (this.canvas.isDrawingMode) {
            this.canvas.freeDrawingBrush.width = this.currentTool === 'eraser' ? width * 3 : width;
        }
    },

    // Add text
    addText: function (text) {
        if (!text) text = 'Text';
        const textObj = new fabric.IText(text, {
            left: this.canvas.width / 2,
            top: this.canvas.height / 2,
            fill: this.currentColor,
            fontSize: 20,
            fontFamily: 'Inter, sans-serif',
            fontWeight: 'bold',
            originX: 'center',
            originY: 'center'
        });
        this.canvas.add(textObj);
        this.canvas.setActiveObject(textObj);
        this.saveState();
    },

    // Save current state for undo/redo
    saveState: function () {
        // Remove future states if we're in the middle of history
        if (this.historyIndex < this.history.length - 1) {
            this.history = this.history.slice(0, this.historyIndex + 1);
        }
        this.history.push(this.canvas.toJSON());
        this.historyIndex = this.history.length - 1;

        // Limit history size
        if (this.history.length > 30) {
            this.history.shift();
            this.historyIndex--;
        }
    },

    // Undo last action
    undo: function () {
        if (this.historyIndex > 0) {
            this.historyIndex--;
            this.canvas.loadFromJSON(this.history[this.historyIndex], () => {
                this.canvas.renderAll();
            });
        }
    },

    // Redo last undone action
    redo: function () {
        if (this.historyIndex < this.history.length - 1) {
            this.historyIndex++;
            this.canvas.loadFromJSON(this.history[this.historyIndex], () => {
                this.canvas.renderAll();
            });
        }
    },

    // Clear all drawings (keep background)
    clear: function () {
        const bg = this.canvas.backgroundImage;
        this.canvas.clear();
        if (bg) {
            this.canvas.setBackgroundImage(bg, this.canvas.renderAll.bind(this.canvas));
        }
        this.saveState();
    },

    // Get annotation data as JSON (for saving/editing later)
    getAnnotationJson: function () {
        return JSON.stringify(this.canvas.toJSON());
    },

    // Load annotation from JSON
    loadAnnotationJson: function (json) {
        return new Promise((resolve) => {
            this.canvas.loadFromJSON(json, () => {
                this.canvas.renderAll();
                this.saveState();
                resolve(true);
            });
        });
    },

    // Export canvas as base64 image
    exportImage: function () {
        return this.canvas.toDataURL({
            format: 'png',
            quality: 0.9
        });
    },

    // Check if canvas has any drawings
    hasDrawings: function () {
        return this.canvas.getObjects().length > 0;
    },

    // Dispose canvas
    dispose: function () {
        if (this.canvas) {
            this.canvas.dispose();
            this.canvas = null;
        }
        this.history = [];
        this.historyIndex = -1;
    }
};
