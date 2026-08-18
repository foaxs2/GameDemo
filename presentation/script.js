/* ============================================================
   ASHBOUND PRESENTATION — Slide Navigation Script
   ============================================================ */

(function () {
  'use strict';

  const TOTAL_SLIDES = 18;
  let currentSlide = 1;
  let isTransitioning = false;
  let touchStartX = 0;

  // DOM refs
  const slides = document.querySelectorAll('.slide');
  const progressFill = document.getElementById('progressFill');
  const slideCounter = document.getElementById('slideCounter');
  const prevBtn = document.getElementById('prevBtn');
  const nextBtn = document.getElementById('nextBtn');
  const keyHint = document.getElementById('keyHint');

  // ── Initialize ──
  function init() {
    updateSlide(1, 'none');
    
    // Keyboard navigation
    document.addEventListener('keydown', handleKeyboard);

    // Touch / swipe
    document.addEventListener('touchstart', handleTouchStart, { passive: true });
    document.addEventListener('touchend', handleTouchEnd, { passive: true });

    // Mouse wheel (debounced)
    let wheelTimeout = null;
    document.addEventListener('wheel', (e) => {
      if (wheelTimeout) return;
      wheelTimeout = setTimeout(() => { wheelTimeout = null; }, 600);
      if (e.deltaY > 30) changeSlide(1);
      else if (e.deltaY < -30) changeSlide(-1);
    }, { passive: true });

    // Hide keyboard hint after 5s
    setTimeout(() => {
      keyHint.classList.add('hidden');
    }, 5000);
  }

  // ── Change Slide ──
  window.changeSlide = function (direction) {
    if (isTransitioning) return;

    const next = currentSlide + direction;
    if (next < 1 || next > TOTAL_SLIDES) return;

    isTransitioning = true;

    const currentEl = slides[currentSlide - 1];
    const nextEl = slides[next - 1];

    // Exit animation
    if (direction > 0) {
      currentEl.classList.remove('slide-active');
      currentEl.classList.add('slide-exit-left');
    } else {
      currentEl.classList.remove('slide-active');
      currentEl.style.transform = 'translateX(60px)';
      currentEl.style.opacity = '0';
    }

    // Enter animation
    if (direction > 0) {
      nextEl.style.transform = 'translateX(60px)';
    } else {
      nextEl.style.transform = 'translateX(-60px)';
      nextEl.classList.add('slide-exit-left');
    }

    // Small delay for CSS to register the start state
    requestAnimationFrame(() => {
      requestAnimationFrame(() => {
        if (direction < 0) {
          nextEl.classList.remove('slide-exit-left');
        }
        nextEl.classList.add('slide-active');
        nextEl.style.transform = '';

        currentSlide = next;
        updateUI();

        setTimeout(() => {
          currentEl.classList.remove('slide-exit-left');
          currentEl.style.transform = '';
          currentEl.style.opacity = '';
          isTransitioning = false;
        }, 500);
      });
    });
  };

  // ── Update UI ──
  function updateUI() {
    const progress = (currentSlide / TOTAL_SLIDES) * 100;
    progressFill.style.width = progress + '%';
    slideCounter.textContent = currentSlide + ' / ' + TOTAL_SLIDES;

    // Nav buttons state
    prevBtn.style.opacity = currentSlide === 1 ? '0.3' : '1';
    prevBtn.style.pointerEvents = currentSlide === 1 ? 'none' : 'auto';
    nextBtn.style.opacity = currentSlide === TOTAL_SLIDES ? '0.3' : '1';
    nextBtn.style.pointerEvents = currentSlide === TOTAL_SLIDES ? 'none' : 'auto';
  }

  function updateSlide(slideNum, direction) {
    slides.forEach((s, i) => {
      s.classList.remove('slide-active', 'slide-exit-left');
      s.style.transform = '';
      s.style.opacity = '';
    });
    slides[slideNum - 1].classList.add('slide-active');
    currentSlide = slideNum;
    updateUI();
  }

  // ── Keyboard ──
  function handleKeyboard(e) {
    switch (e.key) {
      case 'ArrowRight':
      case 'ArrowDown':
      case ' ':
      case 'PageDown':
        e.preventDefault();
        changeSlide(1);
        break;
      case 'ArrowLeft':
      case 'ArrowUp':
      case 'PageUp':
        e.preventDefault();
        changeSlide(-1);
        break;
      case 'Home':
        e.preventDefault();
        if (currentSlide !== 1) {
          updateSlide(1);
        }
        break;
      case 'End':
        e.preventDefault();
        if (currentSlide !== TOTAL_SLIDES) {
          updateSlide(TOTAL_SLIDES);
        }
        break;
    }
  }

  // ── Touch / Swipe ──
  function handleTouchStart(e) {
    touchStartX = e.changedTouches[0].screenX;
  }

  function handleTouchEnd(e) {
    const touchEndX = e.changedTouches[0].screenX;
    const diff = touchStartX - touchEndX;
    if (Math.abs(diff) > 50) {
      changeSlide(diff > 0 ? 1 : -1);
    }
  }

  // Start
  init();
})();
