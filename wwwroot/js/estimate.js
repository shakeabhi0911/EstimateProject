/**
 * Estimate Pending – Cost Management System
 * Client-side JavaScript | jQuery + Bootstrap
 * ASP.NET Core MVC Project – Tata Steel UISL
 */

$(function () {

    /* ── constants ──────────────────────────────────────────── */
    const SAVE_URL    = '/Estimate/SaveRows';
    const VERIFY_URL  = '/Estimate/Verify';
    const SUBMIT_URL  = '/Estimate/Submit';
    const MASTER_URL  = '/Estimate/GetMasterItems';

    let masterItems = [];  // cache of material master

    /* ── Load master items for autocomplete ─────────────────── */
    $.getJSON(MASTER_URL, function (data) {
        masterItems = data || [];
    });

    /* ══ FILE IMPORT ═══════════════════════════════════════════ */
    $('#excelFileInput').on('change', function () {
        if (this.files && this.files.length > 0) {
            $('#importForm')[0].submit();
        }
    });

    /* ══ ADD ITEM (manual blank row) ═══════════════════════════ */
    $('#btnAddItem').on('click', function () {
        addBlankRow();
        showEmptyStateOff();
    });

    function addBlankRow() {
        const serial  = $('#estimateBody tr').length + 1;
        const newRow  = buildRow({
            SerialNo: serial,
            ItemDesc: '', UOM: '',
            Quantity: '', MaterialCost: '', ServiceCost: '',
            TotalCost: 0, IsVerified: false, MismatchFlag: false
        });
        $('#estimateBody').append(newRow);
        bindRowEvents($('#estimateBody tr:last'));
        renumberRows();
    }

    /* ══ BUILD ROW HTML ════════════════════════════════════════ */
    function buildRow(r) {
        const statusBadge = r.MismatchFlag
            ? `<span class="badge badge-mismatch" title="${escHtml(r.MismatchReason || '')}">
                   <i class="fas fa-exclamation-triangle"></i> Mismatch
               </span>`
            : r.IsVerified
            ? `<span class="badge badge-ok"><i class="fas fa-check"></i> Verified</span>`
            : `<span class="badge badge-pending">Pending</span>`;

        const rowClass = r.MismatchFlag ? 'row-mismatch' : r.IsVerified ? 'row-ok' : '';

        const total = r.TotalCost
            ? '₹' + parseFloat(r.TotalCost).toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ',')
            : '₹0.00';

        return `
        <tr class="est-row ${rowClass}" data-serial="${r.SerialNo}">
            <td class="col-sno sno-cell">${r.SerialNo}</td>
            <td class="col-desc">
                <input type="text" class="form-control form-control-sm cell-input inp-desc"
                       value="${escHtml(r.ItemDesc)}" placeholder="Item description" />
            </td>
            <td class="col-uom">
                <input type="text" class="form-control form-control-sm cell-input inp-uom"
                       value="${escHtml(r.UOM)}" placeholder="KG/NOS" style="max-width:80px"/>
            </td>
            <td class="col-qty">
                <input type="number" step="0.001" min="0"
                       class="form-control form-control-sm cell-input inp-qty"
                       value="${r.Quantity}" placeholder="0" />
            </td>
            <td class="col-mat">
                <input type="number" step="0.01" min="0"
                       class="form-control form-control-sm cell-input inp-mat"
                       value="${r.MaterialCost}" placeholder="0.00" />
            </td>
            <td class="col-svc">
                <input type="number" step="0.01" min="0"
                       class="form-control form-control-sm cell-input inp-svc"
                       value="${r.ServiceCost}" placeholder="0.00" />
            </td>
            <td class="col-tot total-cell">${total}</td>
            <td class="col-status status-cell">${statusBadge}</td>
            <td class="col-action">
                <button class="btn btn-sm btn-remove-row" title="Remove row">
                    <i class="fas fa-times"></i>
                </button>
            </td>
        </tr>`;
    }

    /* ══ BIND ROW EVENTS ═══════════════════════════════════════ */
    function bindRowEvents($row) {
        // Remove row
        $row.find('.btn-remove-row').off('click').on('click', function () {
            $(this).closest('tr').remove();
            renumberRows();
            updateGrandTotal();
            autoSave();
            if ($('#estimateBody tr').length === 0) showEmptyStateOn();
        });

        // Recalculate total on input change
        $row.find('.inp-qty, .inp-mat, .inp-svc').off('input').on('input', function () {
            recalcRow($(this).closest('tr'));
            updateGrandTotal();
            clearVerifyStatus();
        });

        // Autocomplete on description
        $row.find('.inp-desc').off('input').on('input', function () {
            const term = $(this).val().toLowerCase();
            const match = masterItems.find(m =>
                m.itemDesc && m.itemDesc.toLowerCase().startsWith(term));
            if (match && term.length > 1) {
                const $tr = $(this).closest('tr');
                $tr.find('.inp-uom').val(match.uom || '');
                $tr.find('.inp-mat').val(match.materialCost || '');
                $tr.find('.inp-svc').val(match.serviceCost  || '');
                recalcRow($tr);
                updateGrandTotal();
            }
            clearVerifyStatus();
        });

        // Auto-save on blur
        $row.find('.cell-input').off('blur').on('blur', function () {
            autoSave();
        });
    }

    // Bind events for rows already rendered server-side
    $('#estimateBody tr').each(function () { bindRowEvents($(this)); });

    /* ══ ROW RECALCULATION ═════════════════════════════════════ */
    function recalcRow($tr) {
        const qty = parseFloat($tr.find('.inp-qty').val()) || 0;
        const mat = parseFloat($tr.find('.inp-mat').val()) || 0;
        const svc = parseFloat($tr.find('.inp-svc').val()) || 0;
        const tot = (qty * mat) + svc;
        $tr.find('.total-cell').text('₹' + tot.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ','));
    }

    /* ══ GRAND TOTAL ═══════════════════════════════════════════ */
    function updateGrandTotal() {
        let total = 0;
        $('#estimateBody tr').each(function () {
            const qty = parseFloat($(this).find('.inp-qty').val()) || 0;
            const mat = parseFloat($(this).find('.inp-mat').val()) || 0;
            const svc = parseFloat($(this).find('.inp-svc').val()) || 0;
            total += (qty * mat) + svc;
        });
        $('#grandTotalCell').text('₹' + total.toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ','));
    }

    /* ══ RENUMBER ROWS ═════════════════════════════════════════ */
    function renumberRows() {
        $('#estimateBody tr').each(function (i) {
            $(this).attr('data-serial', i + 1);
            $(this).find('.sno-cell').text(i + 1);
        });
    }

    /* ══ COLLECT ROWS TO JSON ══════════════════════════════════ */
    function collectRows() {
        const rows = [];
        $('#estimateBody tr').each(function (i) {
            const $tr = $(this);
            const qty = parseFloat($tr.find('.inp-qty').val()) || 0;
            const mat = parseFloat($tr.find('.inp-mat').val()) || 0;
            const svc = parseFloat($tr.find('.inp-svc').val()) || 0;
            rows.push({
                SerialNo:     i + 1,
                ItemDesc:     $tr.find('.inp-desc').val().trim(),
                UOM:          $tr.find('.inp-uom').val().trim(),
                Quantity:     qty,
                MaterialCost: mat,
                ServiceCost:  svc,
                TotalCost:    (qty * mat) + svc,
                IsVerified:   $tr.hasClass('row-ok'),
                MismatchFlag: $tr.hasClass('row-mismatch'),
                MismatchReason: ''
            });
        });
        return rows;
    }

    /* ══ AUTO-SAVE ═════════════════════════════════════════════ */
    let saveTimer;
    function autoSave() {
        clearTimeout(saveTimer);
        saveTimer = setTimeout(function () {
            const rows = collectRows();
            if (!rows.length) return;
            $.ajax({
                url: SAVE_URL, method: 'POST',
                contentType: 'application/json',
                data: JSON.stringify(rows)
            });
        }, 800);
    }

    /* ══ VERIFY ════════════════════════════════════════════════ */
    $('#btnVerify').on('click', function () {
        const rows = collectRows();
        if (!rows.length) return showToast('No rows to verify.', 'warning');

        showLoader(true);
        $.ajax({
            url: VERIFY_URL, method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(rows),
            success: function (res) {
                showLoader(false);
                if (!res.success) return showToast(res.message, 'danger');

                // Re-render rows with verification state
                $('#estimateBody').empty();
                res.rows.forEach(function (r) {
                    const row = {
                        SerialNo:      r.serialNo,
                        ItemDesc:      r.itemDesc,
                        UOM:           r.uom,
                        Quantity:      r.quantity,
                        MaterialCost:  r.materialCost,
                        ServiceCost:   r.serviceCost,
                        TotalCost:     r.totalCost,
                        IsVerified:    r.isVerified,
                        MismatchFlag:  r.mismatchFlag,
                        MismatchReason: r.mismatchReason
                    };
                    const $tr = $(buildRow(row));
                    $('#estimateBody').append($tr);
                    bindRowEvents($tr);
                });

                updateGrandTotal();

                // Status bar
                const $bar = $('#verifyStatusBar').removeClass('d-none has-errors all-ok');
                if (res.mismatchCount > 0) {
                    $bar.addClass('has-errors');
                    $('#verifyMsg').html(
                        `<i class="fas fa-exclamation-triangle mr-1"></i>
                         Verification complete: <strong>${res.mismatchCount}</strong> mismatch(es) found.
                         ${res.verifiedCount} row(s) verified. Please review highlighted rows.`);
                } else {
                    $bar.addClass('all-ok');
                    $('#verifyMsg').html(
                        `<i class="fas fa-check-circle mr-1"></i>
                         All <strong>${res.verifiedCount}</strong> rows verified successfully!`);
                }

                if (!res.dbAvailable) {
                    showToast('Database unavailable – formula-only validation applied.', 'warning');
                }
            },
            error: function () {
                showLoader(false);
                showToast('Verification failed. Please try again.', 'danger');
            }
        });
    });

    /* ══ SUBMIT ════════════════════════════════════════════════ */
    $('#btnSubmit').on('click', function () {
        const rows = collectRows();
        if (!rows.length) return showToast('No data to submit.', 'warning');

        const emptyDescs = rows.filter(r => !r.ItemDesc);
        if (emptyDescs.length) {
            return showToast(`${emptyDescs.length} row(s) have empty description. Please fill all fields.`, 'warning');
        }

        if (!confirm('Submit this estimate to the database?')) return;

        showLoader(true);
        $.ajax({
            url: SUBMIT_URL, method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(rows),
            success: function (res) {
                showLoader(false);
                if (res.success) {
                    $('#estimateBody').empty();
                    $('#emptyState').removeClass('d-none');
                    $('#tableWrapper').addClass('d-none');
                    updateGrandTotal();
                    $('#verifyStatusBar').addClass('d-none');

                    // Show success modal
                    $('#modalEstNo').text(res.estimateNo);
                    $('#modalMsg').text(res.message);
                    $('#modalTotal').text('Total: ₹' + parseFloat(res.totalCost)
                        .toFixed(2).replace(/\B(?=(\d{3})+(?!\d))/g, ','));
                    $('#successModal').modal('show');
                } else {
                    showToast(res.message || 'Submission failed.', 'danger');
                }
            },
            error: function () {
                showLoader(false);
                showToast('Submission failed. Please try again.', 'danger');
            }
        });
    });

    /* ══ REMOVE ALL ════════════════════════════════════════════ */
    $('#btnRemoveAll').on('click', function () {
        if (!confirm('Remove all estimate data? This cannot be undone.')) return;
        window.location.href = '/Estimate/Remove';
    });

    /* ══ HELPERS ═══════════════════════════════════════════════ */
    function showLoader(on) {
        if (on) $('#tableLoader').removeClass('d-none');
        else     $('#tableLoader').addClass('d-none');
    }

    function showEmptyStateOn() {
        $('#emptyState').removeClass('d-none');
        $('#tableWrapper').addClass('d-none');
    }
    function showEmptyStateOff() {
        $('#emptyState').addClass('d-none');
        $('#tableWrapper').removeClass('d-none');
    }

    function clearVerifyStatus() {
        $('#verifyStatusBar').addClass('d-none');
    }

    function showToast(msg, type) {
        const colors = {
            success: '#27ae60', danger: '#e74c3c',
            warning: '#f39c12', info: '#17a2b8'
        };
        const $t = $(`
            <div style="
                position:fixed; bottom:24px; right:24px; z-index:9999;
                background:${colors[type] || colors.info}; color:#fff;
                padding:12px 20px; border-radius:6px;
                box-shadow:0 4px 16px rgba(0,0,0,.2);
                font-weight:600; font-size:.88rem;
                animation: fadeInUp .3s ease;
            ">${msg}</div>`);
        $('body').append($t);
        setTimeout(() => $t.fadeOut(400, () => $t.remove()), 4000);
    }

    function escHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    /* ══ INIT ══════════════════════════════════════════════════ */
    updateGrandTotal();

    // Add CSS animation for toast
    $('<style>@keyframes fadeInUp{from{opacity:0;transform:translateY(12px)}to{opacity:1;transform:translateY(0)}}</style>')
        .appendTo('head');
});
