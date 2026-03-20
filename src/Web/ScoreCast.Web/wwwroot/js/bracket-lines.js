window.bracketLines = {
    _resizeHandler: null,
    draw: function (containerId) {
        if (this._resizeHandler) {
            window.removeEventListener('resize', this._resizeHandler);
        }

        this._drawLines(containerId);

        var self = this;
        var timeout;
        this._resizeHandler = function () {
            clearTimeout(timeout);
            timeout = setTimeout(function () {
                self.scaleToFit(containerId, 'bracket-inner');
                self._drawLines(containerId);
            }, 16);
        };
        window.addEventListener('resize', this._resizeHandler);
    },
    scaleToFit: function (containerId, innerId) {
        var container = document.getElementById(containerId);
        var inner = document.getElementById(innerId);
        if (!container || !inner) return;

        inner.style.transform = 'none';
        container.style.height = 'auto';
        var cw = container.clientWidth;
        var iw = inner.scrollWidth;
        if (iw > cw && cw > 0) {
            var scale = cw / iw;
            inner.style.transform = 'scale(' + scale + ')';
            container.style.height = (inner.scrollHeight * scale) + 'px';
        }
    },
    _drawLines: function (containerId) {
        var container = document.getElementById(containerId);
        if (!container) return;

        var old = container.querySelector('.bracket-svg-overlay');
        if (old) old.remove();

        var rect = container.getBoundingClientRect();
        var scrollLeft = container.scrollLeft;
        var scrollTop = container.scrollTop;

        var svg = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
        svg.classList.add('bracket-svg-overlay');
        svg.style.cssText = 'position:absolute;top:0;left:0;pointer-events:none;overflow:visible;';
        svg.setAttribute('width', container.scrollWidth);
        svg.setAttribute('height', container.scrollHeight);

        var colMap = {};
        container.querySelectorAll('[data-bracket-col]').forEach(function (col) {
            var idx = col.getAttribute('data-bracket-col');
            colMap[idx] = [];
            col.querySelectorAll('[data-bracket-card]').forEach(function (card) {
                var cr = card.getBoundingClientRect();
                colMap[idx].push({
                    midY: (cr.top + cr.bottom) / 2 - rect.top + scrollTop,
                    left: cr.left - rect.left + scrollLeft,
                    right: cr.right - rect.left + scrollLeft
                });
            });
        });

        drawMerge(svg, colMap, '0', '1', 'right');
        drawMerge(svg, colMap, '1', '2', 'right');
        drawMerge(svg, colMap, '2', '3', 'right');
        drawStraight(svg, colMap, '3', '4', 'right');

        drawMerge(svg, colMap, '8', '7', 'left');
        drawMerge(svg, colMap, '7', '6', 'left');
        drawMerge(svg, colMap, '6', '5', 'left');
        drawStraight(svg, colMap, '5', '4', 'left');

        container.appendChild(svg);
    }
};

function drawMerge(svg, colMap, srcIdx, dstIdx, direction) {
    var src = colMap[srcIdx];
    var dst = colMap[dstIdx];
    if (!src || !dst || src.length === 0 || dst.length === 0) return;

    for (var d = 0; d < dst.length; d++) {
        var s1 = src[d * 2];
        var s2 = src[d * 2 + 1];
        var dt = dst[d];
        if (!s1 || !s2 || !dt) continue;

        if (direction === 'right') {
            var midX = (s1.right + dt.left) / 2;
            line(svg, s1.right, s1.midY, midX, s1.midY);
            line(svg, s2.right, s2.midY, midX, s2.midY);
            line(svg, midX, s1.midY, midX, s2.midY);
            line(svg, midX, (s1.midY + s2.midY) / 2, dt.left, dt.midY);
        } else {
            var midX = (s1.left + dt.right) / 2;
            line(svg, s1.left, s1.midY, midX, s1.midY);
            line(svg, s2.left, s2.midY, midX, s2.midY);
            line(svg, midX, s1.midY, midX, s2.midY);
            line(svg, midX, (s1.midY + s2.midY) / 2, dt.right, dt.midY);
        }
    }
}

function drawStraight(svg, colMap, srcIdx, dstIdx, direction) {
    var src = colMap[srcIdx];
    var dst = colMap[dstIdx];
    if (!src || !dst || src.length === 0 || dst.length === 0) return;

    for (var i = 0; i < Math.min(src.length, dst.length); i++) {
        if (direction === 'right') {
            line(svg, src[i].right, src[i].midY, dst[i].left, dst[i].midY);
        } else {
            line(svg, src[i].left, src[i].midY, dst[i].right, dst[i].midY);
        }
    }
}

function line(svg, x1, y1, x2, y2) {
    var l = document.createElementNS('http://www.w3.org/2000/svg', 'line');
    l.setAttribute('x1', x1);
    l.setAttribute('y1', y1);
    l.setAttribute('x2', x2);
    l.setAttribute('y2', y2);
    l.setAttribute('stroke', '#999');
    l.setAttribute('stroke-width', '1.5');
    svg.appendChild(l);
}
