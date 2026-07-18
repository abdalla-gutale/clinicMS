// Packages JS
var packages = [
    {id:1,name:'Basic Monthly',price:300,duration:1,visits:2,desc:'Access to gym facilities twice per day. Perfect for beginners.',status:'Active'},
    {id:2,name:'Pro Monthly',price:500,duration:1,visits:0,desc:'Unlimited daily visits with access to all equipment and classes.',status:'Active'},
    {id:3,name:'Elite Monthly',price:800,duration:1,visits:0,desc:'Unlimited visits plus personal trainer session twice a week.',status:'Active'},
    {id:4,name:'Basic 3-Month',price:800,duration:3,visits:2,desc:'Three months of twice-daily access at a discounted rate.',status:'Active'},
    {id:5,name:'Pro 6-Month',price:2500,duration:6,visits:0,desc:'Six months unlimited access with bonus nutrition consultation.',status:'Active'},
    {id:6,name:'Annual',price:4500,duration:12,visits:0,desc:'Full year unlimited access. Best value membership package.',status:'Active'}
];

var editingId=null;

var gradients = [
    'linear-gradient(135deg,#0d9488,#2dd4bf)',
    'linear-gradient(135deg,#2563eb,#60a5fa)',
    'linear-gradient(135deg,#7c3aed,#a78bfa)',
    'linear-gradient(135deg,#dc2626,#f87171)',
    'linear-gradient(135deg,#d97706,#fbbf24)',
    'linear-gradient(135deg,#16a34a,#4ade80)'
];

function renderGrid() {
    var grid=document.getElementById('packagesGrid');
    grid.innerHTML=packages.map((p,i)=>`
        <div class="col-12 col-md-6 col-lg-4">
            <div class="pkg-card">
                <span class="pkg-badge ${p.status==='Active'?'pkg-badge-active':'pkg-badge-inactive'}">${p.status}</span>
                <div style="width:44px;height:44px;border-radius:12px;background:${gradients[i%gradients.length]};display:flex;align-items:center;justify-content:center;margin-bottom:12px;">
                    <i class="ri-vip-crown-line" style="color:#fff;font-size:1.2rem;"></i>
                </div>
                <div class="pkg-name">${p.name}</div>
                <div class="pkg-price">${p.price.toLocaleString()}<span> EGP</span></div>
                <div class="pkg-meta">
                    <span class="pkg-pill"><i class="ri-calendar-line me-1"></i>${p.duration} month${p.duration>1?'s':''}</span>
                    <span class="pkg-pill"><i class="ri-run-line me-1"></i>${p.visits===0?'Unlimited':p.visits+' visits/day'}</span>
                </div>
                <div class="pkg-desc">${p.desc}</div>
                <div class="pkg-actions">
                    <button class="btn btn-sm px-3 py-1 fw-600" style="background:#f0fdfa;color:#0d9488;border-radius:8px;" onclick="openModal(${p.id})">
                        <i class="ri-pencil-line me-1"></i>Edit
                    </button>
                    <button class="btn btn-sm px-3 py-1 fw-600" style="background:#fef2f2;color:#dc2626;border-radius:8px;" onclick="deletePackage(${p.id})">
                        <i class="ri-delete-bin-line me-1"></i>Delete
                    </button>
                </div>
            </div>
        </div>`).join('');
}

function openModal(id) {
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Package':'Add Package';
    if(id){
        var p=packages.find(x=>x.id===id);
        document.getElementById('fName').value=p.name;
        document.getElementById('fPrice').value=p.price;
        document.getElementById('fDuration').value=p.duration;
        document.getElementById('fVisits').value=p.visits;
        document.getElementById('fDesc').value=p.desc;
        document.getElementById('fStatus').checked=p.status==='Active';
    } else {
        ['fName','fDesc'].forEach(f=>document.getElementById(f).value='');
        document.getElementById('fPrice').value='';
        document.getElementById('fDuration').value='1';
        document.getElementById('fVisits').value='0';
        document.getElementById('fStatus').checked=true;
    }
    new bootstrap.Modal(document.getElementById('pkgModal')).show();
}

function savePackage() {
    var name=document.getElementById('fName').value.trim();
    var price=parseFloat(document.getElementById('fPrice').value);
    if(!name||isNaN(price)){toastr.error('Fill required fields');return;}
    var data={id:editingId||Date.now(),name,price,
        duration:parseInt(document.getElementById('fDuration').value)||1,
        visits:parseInt(document.getElementById('fVisits').value)||0,
        desc:document.getElementById('fDesc').value,
        status:document.getElementById('fStatus').checked?'Active':'Inactive'};
    if(editingId){var idx=packages.findIndex(x=>x.id===editingId);packages[idx]=data;toastr.success('Package updated');}
    else{packages.push(data);toastr.success('Package added');}
    bootstrap.Modal.getInstance(document.getElementById('pkgModal')).hide();
    renderGrid();
}

function deletePackage(id){if(!confirm('Delete?'))return;packages=packages.filter(p=>p.id!==id);toastr.success('Deleted');renderGrid();}
document.addEventListener('DOMContentLoaded',renderGrid);

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
